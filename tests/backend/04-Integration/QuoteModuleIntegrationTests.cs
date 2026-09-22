using System.Net;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Worker;
using static Voltflow.Tests.TestApi;

namespace Voltflow.Tests;

// The quote module end to end through the API: who can do what, what is refused, how a quote moves from draft to work order,
// and how the daily sweep retires the offers nobody answered. Every test brings its own users and customers (tagged), so
// none depends on another; the clock is the fixture's fake one, which the tests move forward when time matters.
[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "02301")]
public sealed class QuoteModuleIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _factory;

    public QuoteModuleIntegrationTests(ApiTestFixture factory) => _factory = factory;

    [Fact]
    [Trait("VUT", "02302")]
    public async Task AQuote_IsNumberedInOrder_PricedWithVatIncluded_AndARefusedOneLeavesNoGap()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var customer = await CustomerAsync(manager);

        var first = await CreateAsync(manager, customer.Id, "Solar roof",
            Line("Fitting", 2, 120, "saat", 20, "Labor"), Line("Cable", 1, 110, "metre", 10, "Material"), Line("Advice", 1, 50, null, 0));

        Assert.StartsWith("TK-", first.Number, StringComparison.Ordinal);
        Assert.Equal(("Draft", customer.FullName, 400m, 50m), (first.State, first.CustomerName, first.Total, first.VatTotal));
        Assert.Equal([1, 2, 3], first.Items.Select(line => line.LineNumber));
        Assert.Equal(["Labor", "Material", "Service"], first.Items.Select(line => line.Kind));
        Assert.Equal(["saat", "metre", "adet"], first.Items.Select(line => line.Unit));
        Assert.Equal([40m, 10m, 0m], first.Items.Select(line => line.VatAmount));

        // everything that is refused is refused before a number is taken
        var today = Today();
        await AssertRefusedAsync(manager, new CreateQuoteRequest(customer.Id, " "), null);
        await AssertRefusedAsync(manager, new CreateQuoteRequest(Guid.NewGuid(), "Nobody's"), "CUSTOMER_NOT_FOUND");
        await AssertRefusedAsync(manager, new CreateQuoteRequest(customer.Id, "Late", ValidUntil: today.AddDays(-1)), "QUOTE_VALIDITY_PAST");
        await AssertRefusedAsync(manager, new CreateQuoteRequest(customer.Id, "Bad line", Items: [Line("Zero", 0, 10)]), null);
        await AssertRefusedAsync(manager, new CreateQuoteRequest(customer.Id, "Bad rate", Items: [Line("Rate", 1, 10, vatRate: 120)]), null);
        await AssertRefusedAsync(manager, new CreateQuoteRequest(customer.Id, "Bad kind", Items: [new QuoteLineRequest("Kind", 1, 10, Kind: "Gift")]), null);
        await AssertRefusedAsync(manager, new CreateQuoteRequest(customer.Id, new string('x', 201)), null);

        var second = await CreateAsync(manager, customer.Id, "Second");
        Assert.Equal(NumberOf(first) + 1, NumberOf(second));

        // a customer who was set aside gets no new quotes
        var inactive = await CustomerAsync(manager);
        await ReadAsync<CustomerDto>(await PostAsync(manager.Client, $"/api/customers/{inactive.Id}/deactivate"));
        await AssertRefusedAsync(manager, new CreateQuoteRequest(inactive.Id, "Set aside"), "CUSTOMER_INACTIVE");
        var third = await CreateAsync(manager, customer.Id, "Third");
        Assert.Equal(NumberOf(second) + 1, NumberOf(third));
    }

    [Fact]
    [Trait("VUT", "02303")]
    public async Task TheList_NarrowsByNumberTitleCustomerAndState_AndPages()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var tag = Tag();
        var acme = await CustomerAsync(manager, $"Acme {tag}");
        var beta = await CustomerAsync(manager, $"Beta {tag}");
        var roof = await CreateAsync(manager, acme.Id, $"Roof {tag}", Line("Panels", 1, 1000));
        var wiring = await CreateAsync(manager, acme.Id, $"Wiring {tag}", Line("Cable", 1, 100));
        var betaRoof = await CreateAsync(manager, beta.Id, $"Roof {tag}", Line("Panels", 1, 500));
        await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{wiring.Id}/issue"));

        Assert.Equal(3, (await ListAsync(manager, $"search={tag}")).Count);
        Assert.Equal(new[] { roof.Id, wiring.Id }.Order(), (await ListAsync(manager, $"search=ACME {tag}")).Select(row => row.Id).Order()); // customer name, in any case
        Assert.Equal(new[] { roof.Id, betaRoof.Id }.Order(), (await ListAsync(manager, $"search=roof {tag}")).Select(row => row.Id).Order()); // the title
        Assert.Equal(wiring.Id, Assert.Single(await ListAsync(manager, $"search={wiring.Number.ToLowerInvariant()}")).Id);
        Assert.Equal(2, (await ListAsync(manager, $"search={tag}&customerId={acme.Id}")).Count);
        Assert.Equal(wiring.Id, Assert.Single(await ListAsync(manager, $"search={tag}&state=Issued")).Id);
        Assert.Equal(2, (await ListAsync(manager, $"search={tag}&state=draft")).Count);
        Assert.Empty(await ListAsync(manager, $"search={tag}&state=Accepted"));

        var row = Assert.Single(await ListAsync(manager, $"search={betaRoof.Number}"));
        Assert.Equal((beta.FullName, betaRoof.Title, "Draft", 500m), (row.CustomerName, row.Title, row.State, row.Total));

        using (var page = await manager.Client.GetAsync($"/api/quotes?search={tag}&limit=2"))
        {
            Assert.Equal("3", page.Headers.GetValues("X-Total-Count").Single());
            Assert.Equal(2, (await ReadAsync<List<QuoteSummaryDto>>(page)).Count);
        }

        using var unknownState = await manager.Client.GetAsync("/api/quotes?state=Nonsense");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownState.StatusCode);
    }

    [Fact]
    [Trait("VUT", "02304")]
    public async Task EveryoneSignedInReadsQuotes_ButOnlyManagersChangeThem()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var technician = await _factory.ActorAsync("Technician");
        var customer = await CustomerAsync(manager);
        var quote = await CreateAsync(manager, customer.Id, "Readable", Line("Line", 1, 100));

        Assert.Contains(await ListAsync(technician, $"search={quote.Number}"), row => row.Id == quote.Id);
        Assert.Equal(quote.Number, (await GetAsync(technician, quote.Id)).Number);

        var line = Line("Extra", 1, 1);
        var attempts = new[]
        {
            await PostAsync(technician.Client, "/api/quotes", new CreateQuoteRequest(customer.Id, "Nope")),
            await PutAsync(technician.Client, $"/api/quotes/{quote.Id}", new UpdateQuoteRequest("Nope", null, null, null, null, [line])),
            await DeleteAsync(technician.Client, $"/api/quotes/{quote.Id}"),
            await PostAsync(technician.Client, $"/api/quotes/{quote.Id}/items", line),
            await PostAsync(technician.Client, $"/api/quotes/{quote.Id}/copy"),
            await PostAsync(technician.Client, $"/api/quotes/{quote.Id}/issue"),
            await PostAsync(technician.Client, $"/api/quotes/{quote.Id}/accept", new AcceptQuoteRequest(null)),
            await PostAsync(technician.Client, $"/api/quotes/{quote.Id}/reject", new RejectQuoteRequest("no")),
            await PostAsync(technician.Client, $"/api/quotes/{quote.Id}/expire"),
            await PostAsync(technician.Client, $"/api/quotes/{quote.Id}/work-order")
        };
        Assert.All(attempts, response => { using (response) Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); });
        Assert.Equal(("Draft", 1), ((await GetAsync(manager, quote.Id)).State, (await GetAsync(manager, quote.Id)).Items.Count));

        using var anonymous = _factory.CreateClient();
        using var noToken = await anonymous.GetAsync("/api/quotes");
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);
    }

    [Fact]
    [Trait("VUT", "02305")]
    public async Task ADraft_IsChangedAsAWhole_DeletedOrIssued_AndAnIssuedQuoteIsLocked()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var customer = await CustomerAsync(manager);
        var quote = await CreateAsync(manager, customer.Id, "Original", Line("One", 1, 100), Line("Two", 2, 50));

        var validUntil = Today().AddDays(30);
        var changed = await ReadAsync<QuoteDto>(await PutAsync(manager.Client, $"/api/quotes/{quote.Id}",
            new UpdateQuoteRequest("  Changed ", " a note ", validUntil, null, null, [Line("Only", 3, 40, "adet", 10)])));
        Assert.Equal(("Changed", "a note", validUntil, 120m), (changed.Title, changed.Notes, changed.ValidUntil, changed.Total));
        Assert.Equal(["Only"], changed.Items.Select(line => line.Description));
        Assert.Equal(quote.Number, changed.Number);

        // one bad line refuses the whole change and leaves the draft as it was
        using var refused = await PutAsync(manager.Client, $"/api/quotes/{quote.Id}",
            new UpdateQuoteRequest("Never", null, null, null, null, [Line("Fine", 1, 1), Line("Bad", -1, 1)]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        var unchanged = await GetAsync(manager, quote.Id);
        Assert.Equal(("Changed", 120m, 1), (unchanged.Title, unchanged.Total, unchanged.Items.Count));
        using var pastValidity = await PutAsync(manager.Client, $"/api/quotes/{quote.Id}", new UpdateQuoteRequest("Late", null, Today().AddDays(-1), null, null, []));
        await AssertRejectedAsync(pastValidity, "QUOTE_VALIDITY_PAST");
        using var unknown = await PutAsync(manager.Client, $"/api/quotes/{Guid.NewGuid()}", new UpdateQuoteRequest("Nobody", null, null, null, null, []));
        await AssertRejectedAsync(unknown, "QUOTE_NOT_FOUND");

        // a line can be added one at a time too
        var added = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/items", Line("Added", 1, 10, "kg", 0, "Material")));
        Assert.Equal(130m, added.Total);
        Assert.Equal([1, 2], added.Items.Select(line => line.LineNumber));

        // an empty draft cannot be issued; a draft that is not wanted can be deleted
        var empty = await CreateAsync(manager, customer.Id, "Empty");
        using var issueEmpty = await PostAsync(manager.Client, $"/api/quotes/{empty.Id}/issue");
        await AssertRejectedAsync(issueEmpty, "QUOTE_EMPTY");
        using var deleted = await DeleteAsync(manager.Client, $"/api/quotes/{empty.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var gone = await manager.Client.GetAsync($"/api/quotes/{empty.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        using var deleteAgain = await DeleteAsync(manager.Client, $"/api/quotes/{empty.Id}");
        await AssertRejectedAsync(deleteAgain, "QUOTE_NOT_FOUND");

        // once issued the quote is locked: its lines are what the customer was offered
        var issued = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/issue"));
        Assert.Equal("Issued", issued.State);
        using var change = await PutAsync(manager.Client, $"/api/quotes/{quote.Id}", new UpdateQuoteRequest("Sneaky", null, null, null, null, [Line("Sneaky", 1, 1)]));
        await AssertRejectedAsync(change, "QUOTE_NOT_DRAFT");
        using var addLine = await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/items", Line("Sneaky", 1, 1));
        await AssertRejectedAsync(addLine, "QUOTE_NOT_DRAFT");
        using var deleteIssued = await DeleteAsync(manager.Client, $"/api/quotes/{quote.Id}");
        await AssertRejectedAsync(deleteIssued, "QUOTE_NOT_DRAFT");
        using var issueAgain = await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/issue");
        await AssertRejectedAsync(issueAgain, "QUOTE_NOT_DRAFT");
        Assert.Equal(("Changed", 130m, 2), ((await GetAsync(manager, quote.Id)).Title, (await GetAsync(manager, quote.Id)).Total, (await GetAsync(manager, quote.Id)).Items.Count));
    }

    [Fact]
    [Trait("VUT", "02306")]
    public async Task TheAddressAndTheDevice_MustBeTheCustomersOwn()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var mine = await CustomerAsync(manager);
        var other = await CustomerAsync(manager);
        var site = await ReadAsync<CustomerSiteDto>(await PostAsync(manager.Client, $"/api/customers/{mine.Id}/sites", new SaveSiteRequest("Villa", "Sahil Cd. 4")));
        var otherSite = await ReadAsync<CustomerSiteDto>(await PostAsync(manager.Client, $"/api/customers/{mine.Id}/sites", new SaveSiteRequest("Depot", "Sanayi 9")));
        var inverter = await ReadAsync<CustomerAssetDto>(await PostAsync(manager.Client, $"/api/customers/{mine.Id}/sites/{site.Id}/assets", new SaveAssetRequest("Inverter", $"SN-{Tag()}", null)));
        var foreignSite = await ReadAsync<CustomerSiteDto>(await PostAsync(manager.Client, $"/api/customers/{other.Id}/sites", new SaveSiteRequest("Not mine", "Elsewhere 1")));

        await AssertRefusedAsync(manager, new CreateQuoteRequest(mine.Id, "Wrong address", SiteId: foreignSite.Id), "SITE_NOT_FOUND");
        await AssertRefusedAsync(manager, new CreateQuoteRequest(mine.Id, "Wrong device", SiteId: otherSite.Id, AssetId: inverter.Id), "ASSET_NOT_FOUND");
        await AssertRefusedAsync(manager, new CreateQuoteRequest(mine.Id, "Device alone", AssetId: inverter.Id), null);

        var quote = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(mine.Id, "Right place", SiteId: site.Id, AssetId: inverter.Id)));
        Assert.Equal(("Villa", "Sahil Cd. 4", "Inverter"), (quote.SiteName, quote.SiteAddress, quote.AssetName));

        using var moveToForeign = await PutAsync(manager.Client, $"/api/quotes/{quote.Id}", new UpdateQuoteRequest("Right place", null, null, foreignSite.Id, null, []));
        await AssertRejectedAsync(moveToForeign, "SITE_NOT_FOUND");
        var cleared = await ReadAsync<QuoteDto>(await PutAsync(manager.Client, $"/api/quotes/{quote.Id}", new UpdateQuoteRequest("Right place", null, null, null, null, [])));
        Assert.Equal((null, null, null), (cleared.SiteId, cleared.AssetId, cleared.AssetName));
    }

    [Fact]
    [Trait("VUT", "02307")]
    public async Task AnAcceptedQuote_TakesDeposits_BecomesOneWorkOrder_AndAnyQuoteCanBeCopiedForARevision()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var customer = await CustomerAsync(manager);
        var site = await ReadAsync<CustomerSiteDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites", new SaveSiteRequest("Shop", "Çarşı 12")));
        var asset = await ReadAsync<CustomerAssetDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites/{site.Id}/assets", new SaveAssetRequest("Panel", $"SN-{Tag()}", null)));
        var quote = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(
            customer.Id, "Shop lighting", "call before coming", SiteId: site.Id, AssetId: asset.Id, Items: [Line("Fitting", 2, 150, "adet", 20, "Labor")])));

        using var earlyDeposit = await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/pay-deposit", new PayQuoteDepositRequest(10));
        await AssertRejectedAsync(earlyDeposit, "QUOTE_NOT_ACCEPTED");
        using var earlyAccept = await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/accept", new AcceptQuoteRequest(null));
        await AssertRejectedAsync(earlyAccept, "QUOTE_NOT_ISSUED");
        using var earlyOrder = await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/work-order");
        await AssertRejectedAsync(earlyOrder, "QUOTE_NOT_ACCEPTED");

        var now = _factory.FakeTimeProvider.GetUtcNow().UtcDateTime;
        var issued = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/issue"));
        Assert.Equal((now, DateOnly.FromDateTime(now).AddDays(15)), (issued.IssuedAt, issued.ValidUntil));

        using var badPercentage = await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/accept", new AcceptQuoteRequest(101));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badPercentage.StatusCode);
        var accepted = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/accept", new AcceptQuoteRequest(50)));
        Assert.Equal(("Accepted", 50m, 150m, now), (accepted.State, accepted.RequiredDepositPercentage, accepted.RequiredDepositAmount, accepted.DecidedAt));

        var partly = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/pay-deposit", new PayQuoteDepositRequest(100)));
        Assert.Equal(100m, partly.DepositPaidAmount);
        using var tooMuch = await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/pay-deposit", new PayQuoteDepositRequest(200.01m));
        await AssertRejectedAsync(tooMuch, "QUOTE_DEPOSIT_TOO_HIGH");
        using var nothing = await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/pay-deposit", new PayQuoteDepositRequest(0));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, nothing.StatusCode);
        var enough = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/pay-deposit", new PayQuoteDepositRequest(200)));
        Assert.Equal(300m, enough.DepositPaidAmount);

        // the work order carries the price, the address and the device, and is made once
        var workOrder = await ReadAsync<WorkOrderDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/work-order"));
        Assert.Equal((300m, customer.Id), (workOrder.Total, workOrder.CustomerId));
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
            var stored = await db.WorkOrders.SingleAsync(candidate => candidate.SourceQuoteId == quote.Id);
            Assert.Equal((workOrder.Id, site.Id, asset.Id), (stored.Id, stored.SiteId, stored.AssetId));
        }

        var converted = await GetAsync(manager, quote.Id);
        Assert.Equal((workOrder.Id, workOrder.Number), (converted.WorkOrderId, converted.WorkOrderNumber));
        var again = await ReadAsync<WorkOrderDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/work-order"));
        Assert.Equal(workOrder.Id, again.Id);

        // a copy is a new draft with the same lines, address and device; the quote it came from is left alone
        var copy = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/copy"));
        Assert.Equal(NumberOf(quote) + 1, NumberOf(copy));
        Assert.Equal(("Draft", "Shop lighting", "call before coming", site.Id, asset.Id, 300m, null, 0m),
            (copy.State, copy.Title, copy.Notes, copy.SiteId, copy.AssetId, copy.Total, copy.ValidUntil, copy.DepositPaidAmount));
        Assert.Equal(["Fitting"], copy.Items.Select(line => line.Description));
        Assert.NotEqual(quote.Items.Single().Id, copy.Items.Single().Id);
        Assert.Equal("Accepted", (await GetAsync(manager, quote.Id)).State);
        using var copyOfNothing = await PostAsync(manager.Client, $"/api/quotes/{Guid.NewGuid()}/copy");
        await AssertRejectedAsync(copyOfNothing, "QUOTE_NOT_FOUND");
    }

    [Fact]
    [Trait("VUT", "02308")]
    public async Task AnIssuedQuote_CanBeRejectedWithAReason_OrWithdrawn_AndADecidedOneIsFinal()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var customer = await CustomerAsync(manager);
        async Task<QuoteDto> IssuedAsync(string title)
        {
            var quote = await CreateAsync(manager, customer.Id, title, Line("Line", 1, 100));
            return await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/issue"));
        }

        var toReject = await IssuedAsync("To reject");
        using var noReason = await PostAsync(manager.Client, $"/api/quotes/{toReject.Id}/reject", new RejectQuoteRequest(" "));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noReason.StatusCode);
        var rejected = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{toReject.Id}/reject", new RejectQuoteRequest("  too dear ")));
        Assert.Equal(("Rejected", "too dear"), (rejected.State, rejected.RejectionReason));
        Assert.NotNull(rejected.DecidedAt);
        using var rejectAgain = await PostAsync(manager.Client, $"/api/quotes/{toReject.Id}/reject", new RejectQuoteRequest("again"));
        await AssertRejectedAsync(rejectAgain, "QUOTE_NOT_ISSUED");
        using var acceptRejected = await PostAsync(manager.Client, $"/api/quotes/{toReject.Id}/accept", new AcceptQuoteRequest(null));
        await AssertRejectedAsync(acceptRejected, "QUOTE_NOT_ISSUED");
        using var expireRejected = await PostAsync(manager.Client, $"/api/quotes/{toReject.Id}/expire");
        await AssertRejectedAsync(expireRejected, "QUOTE_ALREADY_DECIDED");

        var toWithdraw = await IssuedAsync("To withdraw");
        var withdrawn = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{toWithdraw.Id}/expire"));
        Assert.Equal("Expired", withdrawn.State);
        using var acceptWithdrawn = await PostAsync(manager.Client, $"/api/quotes/{toWithdraw.Id}/accept", new AcceptQuoteRequest(null));
        await AssertRejectedAsync(acceptWithdrawn, "QUOTE_NOT_ISSUED");

        var toAccept = await IssuedAsync("To accept");
        await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{toAccept.Id}/accept", new AcceptQuoteRequest(null)));
        using var expireAccepted = await PostAsync(manager.Client, $"/api/quotes/{toAccept.Id}/expire");
        await AssertRejectedAsync(expireAccepted, "QUOTE_ALREADY_DECIDED");
        using var rejectAccepted = await PostAsync(manager.Client, $"/api/quotes/{toAccept.Id}/reject", new RejectQuoteRequest("late"));
        await AssertRejectedAsync(rejectAccepted, "QUOTE_NOT_ISSUED");
    }

    [Fact]
    [Trait("VUT", "02309")]
    public async Task AQuote_CannotBeAcceptedAfterItsLastDay_AndTheDailySweepExpiresTheOnesNobodyAnswered()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var customer = await CustomerAsync(manager);
        var today = Today();
        async Task<QuoteDto> IssuedAsync(string title, DateOnly? validUntil)
        {
            var quote = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(customer.Id, title, ValidUntil: validUntil, Items: [Line("Line", 1, 100)])));
            return await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/issue"));
        }

        var shortLived = await IssuedAsync("Short lived", today.AddDays(3));
        var alsoShort = await IssuedAsync("Also short", today.AddDays(3));
        var answered = await IssuedAsync("Answered in time", today.AddDays(3));
        var longLived = await IssuedAsync("Long lived", validUntil: null);
        var draftWithDate = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(customer.Id, "Draft with a date", ValidUntil: today.AddDays(3))));
        await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{answered.Id}/accept", new AcceptQuoteRequest(null)));

        // on its last day a quote can still be accepted; the next day it cannot
        _factory.FakeTimeProvider.Advance(TimeSpan.FromDays(3));
        var lastDay = await IssuedAsync("Issued on the last day of the others", today.AddDays(3));
        await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{lastDay.Id}/accept", new AcceptQuoteRequest(null)));
        _factory.FakeTimeProvider.Advance(TimeSpan.FromDays(1));
        using var late = await PostAsync(manager.Client, $"/api/quotes/{shortLived.Id}/accept", new AcceptQuoteRequest(null));
        await AssertRejectedAsync(late, "QUOTE_EXPIRED");
        Assert.Equal("Issued", (await GetAsync(manager, shortLived.Id)).State);

        // the sweep retires what nobody answered, and only that
        var swept = await SweepAsync();
        Assert.True(swept >= 2, $"Expected at least the two short-lived quotes to expire, but {swept} did.");
        var expired = await GetAsync(manager, shortLived.Id);
        Assert.Equal(("Expired", _factory.FakeTimeProvider.GetUtcNow().UtcDateTime), (expired.State, expired.DecidedAt));
        Assert.Equal("Expired", (await GetAsync(manager, alsoShort.Id)).State);
        Assert.Equal("Accepted", (await GetAsync(manager, answered.Id)).State);
        Assert.Equal("Issued", (await GetAsync(manager, longLived.Id)).State);
        Assert.Equal("Draft", (await GetAsync(manager, draftWithDate.Id)).State);
        Assert.Equal(0, await SweepAsync());

        using var acceptExpired = await PostAsync(manager.Client, $"/api/quotes/{shortLived.Id}/accept", new AcceptQuoteRequest(null));
        await AssertRejectedAsync(acceptExpired, "QUOTE_NOT_ISSUED");
    }

    [Fact]
    public async Task AcceptAsync_ShouldHandleConcurrency_WhenTwoRequestsArriveSimultaneously()
    {
        // Q10: Concurrency test
        using var manager = await _factory.ActorAsync("Manager");
        var customer = await CustomerAsync(manager);
        
        var quoteResponse = await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(customer.Id, "Concurrent Target", Items: new[] { Line("Line", 1, 100) }));
        var quote = await ReadAsync<QuoteDto>(quoteResponse);
        await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/issue");

        // Fire two accept requests concurrently with different payloads to simulate two different actor clicks or races
        var request1 = new HttpRequestMessage(HttpMethod.Post, $"/api/quotes/{quote.Id}/accept")
        {
            Content = JsonContent.Create(new AcceptQuoteRequest(10))
        };
        var request2 = new HttpRequestMessage(HttpMethod.Post, $"/api/quotes/{quote.Id}/accept")
        {
            Content = JsonContent.Create(new AcceptQuoteRequest(20))
        };

        // Ensure distinct Command-Id to bypass idempotency cache and force a real DB race
        request1.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        request2.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        
        var t1 = manager.Client.SendAsync(request1);
        var t2 = manager.Client.SendAsync(request2);

        var responses = await Task.WhenAll(t1, t2);

        var okCount = responses.Count(r => r.IsSuccessStatusCode);
        var conflictOrRejectedCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict || r.StatusCode == HttpStatusCode.UnprocessableEntity);

        if (okCount != 1)
        {
            throw new Exception($"Expected 1 OK, got {okCount}. Statuses: {responses[0].StatusCode} ({await responses[0].Content.ReadAsStringAsync()}), {responses[1].StatusCode} ({await responses[1].Content.ReadAsStringAsync()})");
        }

        if (conflictOrRejectedCount != 1)
        {
            throw new Exception($"Expected 1 Conflict/Rejected, got {conflictOrRejectedCount}. Statuses: {responses[0].StatusCode} ({await responses[0].Content.ReadAsStringAsync()}), {responses[1].StatusCode} ({await responses[1].Content.ReadAsStringAsync()})");
        }

        Assert.Equal(1, okCount);
        Assert.Equal(1, conflictOrRejectedCount);

        // Verify the database state is consistent
        var finalQuote = await GetAsync(manager, quote.Id);
        Assert.Equal("Accepted", finalQuote.State);
    }

    private async Task<int> SweepAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var processor = new QuoteExpiryProcessor(scope.ServiceProvider.GetRequiredService<IQuoteRepository>(), NullLogger<QuoteExpiryProcessor>.Instance);
        return await processor.ExpireDueAsync(_factory.FakeTimeProvider.GetUtcNow().UtcDateTime);
    }

    private DateOnly Today() => DateOnly.FromDateTime(_factory.FakeTimeProvider.GetUtcNow().UtcDateTime);

    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    private static long NumberOf(QuoteDto quote) => long.Parse(quote.Number["TK-".Length..]);

    private static QuoteLineRequest Line(string description, decimal quantity, decimal unitPrice, string? unit = null, decimal? vatRate = null, string? kind = null)
        => new(description, quantity, unitPrice, unit, vatRate, kind);

    private static async Task<CustomerDto> CustomerAsync(Actor manager, string? name = null)
        => await ReadAsync<CustomerDto>(await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest(name ?? $"Customer {Tag()}", null, "5550000")));

    private static async Task<QuoteDto> CreateAsync(Actor manager, Guid customerId, string title, params QuoteLineRequest[] lines)
        => await ReadAsync<QuoteDto>(await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(customerId, title, Items: lines)));

    private static async Task<QuoteDto> GetAsync(Actor actor, Guid id)
        => await ReadAsync<QuoteDto>(await actor.Client.GetAsync($"/api/quotes/{id}"));

    private static async Task<List<QuoteSummaryDto>> ListAsync(Actor actor, string query)
        => await ReadAsync<List<QuoteSummaryDto>>(await actor.Client.GetAsync($"/api/quotes?{query}&limit=100"));

    /// <summary>The API refuses the request on a rule: 422, and the given error code when there is one.</summary>
    private static async Task AssertRefusedAsync(Actor manager, CreateQuoteRequest request, string? errorCode)
    {
        using var response = await PostAsync(manager.Client, "/api/quotes", request);
        if (errorCode is not null) await AssertRejectedAsync(response, errorCode);
        else Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
