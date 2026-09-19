using System;
using Xunit;
using FluentAssertions;
using Voltflow.Domain.Quotes;

namespace Voltflow.Tests.Domain.Quotes;

public class QuoteTests
{
    [Fact]
    public void Constructor_ShouldCreateDraftQuote()
    {
        var customerId = Guid.NewGuid();
        var sut = new Quote(customerId, "Test Quote");

        sut.CustomerId.Should().Be(customerId);
        sut.Title.Should().Be("Test Quote");
        sut.State.Should().Be(QuoteState.Draft);
        sut.Number.Should().StartWith("Q-");
    }

    [Fact]
    public void AddItem_ShouldRecalculateTotal()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");
        
        sut.AddItem("Item 1", 2, 100);
        sut.AddItem("Item 2", 1, 50);

        sut.Total.Should().Be(250);
        sut.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Issue_WithoutItems_ShouldThrowException()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");

        var action = () => sut.Issue();
        
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public void Issue_WithItems_ShouldTransitionToIssued()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");
        sut.AddItem("Item 1", 1, 100);

        sut.Issue();

        sut.State.Should().Be(QuoteState.Issued);
    }

    [Fact]
    public void Accept_WhenIssued_ShouldTransitionToAccepted()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");
        sut.AddItem("Item 1", 1, 100);
        sut.Issue();

        sut.Accept(20); // 20% deposit

        sut.State.Should().Be(QuoteState.Accepted);
        sut.RequiredDepositPercentage.Should().Be(20);
        sut.RequiredDepositAmount.Should().Be(20); // 20% of 100
    }

    [Fact]
    public void PayDeposit_ShouldIncreaseDepositPaidAmount()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");
        sut.AddItem("Item 1", 1, 100);
        sut.Issue();
        sut.Accept(20);

        sut.PayDeposit(20);

        sut.DepositPaidAmount.Should().Be(20);
    }

    // ---- prices are what the customer pays: VAT is inside them --------------------------------------------------

    [Fact]
    public void ThePrices_AreVatInclusive_AndTheVatOfEachLineIsWorkedOutFromItsOwnRate()
    {
        var sut = new Quote(Guid.NewGuid(), "Mixed rates");

        sut.AddItem("Labour", 2, 120, "saat", vatRate: 20, kind: QuoteItemKind.Labor);
        sut.AddItem("Cable", 1, 110, "metre", vatRate: 10, kind: QuoteItemKind.Material);
        sut.AddItem("Consultancy", 1, 50, vatRate: 0);

        sut.Total.Should().Be(400);
        sut.Items.Select(item => item.VatAmount).Should().Equal(40m, 10m, 0m);
        sut.VatTotal.Should().Be(50);
        sut.Items.Select(item => item.LineNumber).Should().Equal(1, 2, 3);
        sut.Items.First().Unit.Should().Be("saat");
        sut.Items.Last().Unit.Should().Be("adet", "a line without a unit is counted in pieces");
    }

    [Fact]
    public void ALine_IsCheckedBeforeAnythingChanges()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");
        sut.AddItem("Kept", 1, 100);

        var tooMany = () => sut.AddItem("Too many", QuoteItem.MaxQuantity + 1, 1);
        var tooDear = () => sut.AddItem("Too dear", 1, QuoteItem.MaxUnitPrice + 1);
        var badRate = () => sut.AddItem("Bad rate", 1, 1, vatRate: 101);
        var fraction = () => sut.AddItem("Fraction", 1.005m, 1);
        var blank = () => sut.AddItem(" ", 1, 1);

        tooMany.Should().Throw<ArgumentOutOfRangeException>();
        tooDear.Should().Throw<ArgumentOutOfRangeException>();
        badRate.Should().Throw<ArgumentOutOfRangeException>();
        fraction.Should().Throw<ArgumentOutOfRangeException>();
        blank.Should().Throw<ArgumentException>();
        sut.Items.Should().ContainSingle();
        sut.Total.Should().Be(100);
    }

    [Fact]
    public void AQuote_HasAtMostTwoHundredLines()
    {
        var sut = new Quote(Guid.NewGuid(), "Big");
        for (var index = 0; index < Quote.MaxItems; index++) sut.AddItem($"Line {index}", 1, 1);

        var oneMore = () => sut.AddItem("One more", 1, 1);
        var replaceWithTooMany = () => sut.Update("Big", null, null, null, null, Enumerable.Repeat(new QuoteItemDraft(QuoteItemKind.Service, "x", 1, 1), Quote.MaxItems + 1));

        oneMore.Should().Throw<InvalidOperationException>().WithMessage("*at most*");
        replaceWithTooMany.Should().Throw<InvalidOperationException>().WithMessage("*at most*");
        sut.Items.Should().HaveCount(Quote.MaxItems);
    }

    // ---- the draft: numbered last, changed as a whole ------------------------------------------------------------

    [Fact]
    public void ADraft_TakesItsNumberOnlyWhenEverythingElseIsValid()
    {
        var numbersTaken = 0;
        string NextNumber() { numbersTaken++; return "TK-000007"; }
        var badLine = new QuoteItemDraft(QuoteItemKind.Service, "Zero", 0, 10);
        var goodLine = new QuoteItemDraft(QuoteItemKind.Material, "  Cable  ", 3, 10, "metre", 10);

        var refused = () => Quote.Create(NextNumber, Guid.NewGuid(), "Refused", null, null, null, null, [goodLine, badLine]);
        var created = Quote.Create(NextNumber, Guid.NewGuid(), "  Accepted  ", "  a note ", new DateOnly(2030, 1, 15), null, null, [goodLine]);

        refused.Should().Throw<ArgumentOutOfRangeException>();
        numbersTaken.Should().Be(1, "only the valid draft used a number up");
        created.Number.Should().Be("TK-000007");
        (created.Title, created.Notes, created.ValidUntil, created.State).Should().Be(("Accepted", "a note", new DateOnly(2030, 1, 15), QuoteState.Draft));
        (created.Items.Single().Description, created.Total).Should().Be(("Cable", 30m));
    }

    [Fact]
    public void ADraft_IsReplacedAsAWhole_AndABadLineChangesNothing()
    {
        var siteId = Guid.NewGuid();
        var sut = Quote.Create(() => "TK-000001", Guid.NewGuid(), "First", "first note", null, siteId, null,
            [new QuoteItemDraft(QuoteItemKind.Service, "Old line", 1, 100)]);
        var oldItemId = sut.Items.Single().Id;

        sut.Update("Second", null, new DateOnly(2030, 2, 1), null, null,
            [new QuoteItemDraft(QuoteItemKind.Labor, "New one", 2, 50), new QuoteItemDraft(QuoteItemKind.Material, "New two", 1, 25)]);

        (sut.Title, sut.Notes, sut.ValidUntil, sut.SiteId, sut.Total).Should().Be(("Second", null, new DateOnly(2030, 2, 1), null, 125m));
        sut.Items.Select(item => item.Description).Should().Equal("New one", "New two");
        sut.Items.Should().NotContain(item => item.Id == oldItemId);

        var refused = () => sut.Update("Third", "x", null, Guid.NewGuid(), null,
            [new QuoteItemDraft(QuoteItemKind.Service, "Fine", 1, 1), new QuoteItemDraft(QuoteItemKind.Service, "Bad", -1, 1)]);
        refused.Should().Throw<ArgumentException>();
        (sut.Title, sut.Total, sut.Items.Count).Should().Be(("Second", 125m, 2));

        var deviceWithoutAddress = () => sut.Update("Third", null, null, null, Guid.NewGuid(), []);
        deviceWithoutAddress.Should().Throw<InvalidOperationException>().WithMessage("*address*");
        sut.Title.Should().Be("Second");
    }

    [Fact]
    public void OnceIssued_TheQuoteCannotBeChanged()
    {
        var sut = new Quote(Guid.NewGuid(), "Locked");
        sut.AddItem("Line", 1, 100);
        sut.Issue();

        var add = () => sut.AddItem("More", 1, 1);
        var update = () => sut.Update("New title", null, null, null, null, []);

        add.Should().Throw<InvalidOperationException>().WithMessage("*draft*");
        update.Should().Throw<InvalidOperationException>().WithMessage("*draft*");
        (sut.Title, sut.Items.Count, sut.Total).Should().Be(("Locked", 1, 100m));
    }

    // ---- validity ------------------------------------------------------------------------------------------------

    [Fact]
    public void Issuing_StampsTheDay_AndValidityDefaultsToFifteenDaysLater()
    {
        var issuedAt = new DateTime(2030, 3, 10, 9, 30, 0, DateTimeKind.Utc);
        var plain = new Quote(Guid.NewGuid(), "Plain");
        plain.AddItem("Line", 1, 100);
        var chosen = Quote.Create(() => "TK-000002", Guid.NewGuid(), "Chosen", null, new DateOnly(2030, 3, 12), null, null, [new QuoteItemDraft(QuoteItemKind.Service, "Line", 1, 1)]);

        plain.Issue(issuedAt);
        chosen.Issue(issuedAt);

        (plain.IssuedAt, plain.ValidUntil).Should().Be((issuedAt, new DateOnly(2030, 3, 25)));
        chosen.ValidUntil.Should().Be(new DateOnly(2030, 3, 12));
    }

    [Fact]
    public void AQuote_CannotBeIssuedWithAValidityDateThatHasAlreadyPassed()
    {
        var sut = Quote.Create(() => "TK-000003", Guid.NewGuid(), "Late", null, new DateOnly(2030, 3, 1), null, null, [new QuoteItemDraft(QuoteItemKind.Service, "Line", 1, 1)]);

        var issue = () => sut.Issue(new DateTime(2030, 3, 2, 8, 0, 0, DateTimeKind.Utc));

        issue.Should().Throw<InvalidOperationException>().WithMessage("*validity*");
        (sut.State, sut.IssuedAt).Should().Be((QuoteState.Draft, null));
    }

    [Fact]
    public void AnIssuedQuote_CanBeAcceptedUpToAndIncludingItsLastDay_ButNotAfter()
    {
        var issuedAt = new DateTime(2030, 3, 1, 8, 0, 0, DateTimeKind.Utc);
        Quote Issued()
        {
            var quote = Quote.Create(() => "TK-000004", Guid.NewGuid(), "Deadline", null, new DateOnly(2030, 3, 5), null, null, [new QuoteItemDraft(QuoteItemKind.Service, "Line", 1, 100)]);
            quote.Issue(issuedAt);
            return quote;
        }

        var onTheLastDay = Issued();
        onTheLastDay.Accept(now: new DateTime(2030, 3, 5, 23, 59, 0, DateTimeKind.Utc));
        var afterwards = Issued();
        var late = () => afterwards.Accept(now: new DateTime(2030, 3, 6, 0, 1, 0, DateTimeKind.Utc));

        onTheLastDay.State.Should().Be(QuoteState.Accepted);
        late.Should().Throw<InvalidOperationException>().WithMessage("*expired*");
        afterwards.State.Should().Be(QuoteState.Issued);
        afterwards.IsPastValidity(new DateOnly(2030, 3, 5)).Should().BeFalse();
        afterwards.IsPastValidity(new DateOnly(2030, 3, 6)).Should().BeTrue();
    }

    [Fact]
    public void TheSweep_ExpiresOnlyIssuedQuotesThatOutlivedTheirValidity()
    {
        var now = new DateTime(2030, 4, 1, 3, 0, 0, DateTimeKind.Utc);
        Quote Make(DateOnly validUntil, bool issue)
        {
            var quote = Quote.Create(() => "TK-000005", Guid.NewGuid(), "Sweep", null, validUntil, null, null, [new QuoteItemDraft(QuoteItemKind.Service, "Line", 1, 100)]);
            if (issue) quote.Issue(new DateTime(2030, 3, 20, 8, 0, 0, DateTimeKind.Utc));
            return quote;
        }

        var outlived = Make(new DateOnly(2030, 3, 31), issue: true);
        var lastDay = Make(new DateOnly(2030, 4, 1), issue: true);
        var draft = Make(new DateOnly(2030, 3, 31), issue: false);

        outlived.ExpireIfDue(now).Should().BeTrue();
        lastDay.ExpireIfDue(now).Should().BeFalse();
        draft.ExpireIfDue(now).Should().BeFalse();

        (outlived.State, outlived.DecidedAt).Should().Be((QuoteState.Expired, now));
        (lastDay.State, draft.State).Should().Be((QuoteState.Issued, QuoteState.Draft));
    }

    // ---- deposits ------------------------------------------------------------------------------------------------

    [Fact]
    public void Deposits_CannotAddUpToMoreThanTheTotal_AndARefusedOneChangesNothing()
    {
        var sut = new Quote(Guid.NewGuid(), "Deposits");
        sut.AddItem("Line", 1, 100);
        sut.Issue();
        sut.Accept(50);
        sut.PayDeposit(60);

        var tooMuch = () => sut.PayDeposit(40.01m);
        var nothing = () => sut.PayDeposit(0.001m);
        sut.PayDeposit(40);

        tooMuch.Should().Throw<InvalidOperationException>().WithMessage("*more than*");
        nothing.Should().Throw<InvalidOperationException>();
        sut.DepositPaidAmount.Should().Be(100, "exactly the total is allowed");
    }

    // ---- revising --------------------------------------------------------------------------------------------------

    [Fact]
    public void ACopy_IsANewDraftWithTheSameLines_AndTheOriginalStaysAsItWas()
    {
        var customerId = Guid.NewGuid();
        var siteId = Guid.NewGuid();
        var original = Quote.Create(() => "TK-000010", customerId, "Solar", "with scaffolding", new DateOnly(2030, 5, 1), siteId, Guid.NewGuid(),
        [
            new QuoteItemDraft(QuoteItemKind.Material, "Panel", 10, 200, "adet", 10),
            new QuoteItemDraft(QuoteItemKind.Labor, "Fitting", 8, 75, "saat", 20)
        ]);
        original.Issue(new DateTime(2030, 4, 20, 8, 0, 0, DateTimeKind.Utc));
        original.Reject("too dear", new DateTime(2030, 4, 22, 8, 0, 0, DateTimeKind.Utc));

        var copy = original.CopyAsDraft(() => "TK-000011");

        copy.Number.Should().Be("TK-000011");
        (copy.CustomerId, copy.Title, copy.Notes, copy.SiteId, copy.AssetId).Should().Be((customerId, "Solar", "with scaffolding", siteId, original.AssetId));
        (copy.State, copy.ValidUntil, copy.IssuedAt, copy.RejectionReason).Should().Be((QuoteState.Draft, null, null, null));
        copy.Items.Select(item => (item.Description, item.Quantity, item.UnitPrice, item.Unit, item.VatRate, item.Kind))
            .Should().Equal(original.Items.Select(item => (item.Description, item.Quantity, item.UnitPrice, item.Unit, item.VatRate, item.Kind)));
        copy.Total.Should().Be(original.Total);
        copy.Items.Select(item => item.Id).Should().NotIntersectWith(original.Items.Select(item => item.Id));
        (original.State, original.Number).Should().Be((QuoteState.Rejected, "TK-000010"));
    }

    [Fact]
    public void ExpiringAndRejecting_LeaveAnAcceptedQuoteAlone()
    {
        var sut = new Quote(Guid.NewGuid(), "Decided");
        sut.AddItem("Line", 1, 100);
        sut.Issue();
        sut.Accept();

        var expire = () => sut.Expire();
        var reject = () => sut.Reject("late");

        expire.Should().Throw<InvalidOperationException>();
        reject.Should().Throw<InvalidOperationException>();
        sut.State.Should().Be(QuoteState.Accepted);
    }
}
