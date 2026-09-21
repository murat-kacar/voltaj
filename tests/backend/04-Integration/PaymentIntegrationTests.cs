using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Identity;
using Voltflow.Domain.WorkOrders;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Tests;

[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "05101")]
public sealed class PaymentIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public PaymentIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<(string adminToken, Customer customer)> SetupAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var admin = new AppUser($"Pay Admin {Guid.NewGuid():N}", $"pay-admin-{Guid.NewGuid():N}@test.com");
        var customer = new Customer($"Pay Cust {Guid.NewGuid():N}", $"pay-cust-{Guid.NewGuid():N}@test.com", "5550002");
        var token = ts.CreateToken(admin, ["Admin"]);
        db.UserSessions.Add(new UserSession(admin.Id, token, DateTime.UtcNow.AddHours(1)));
        db.AddRange(admin, customer);
        await db.SaveChangesAsync();
        return (token, customer);
    }

    private async Task<WorkOrder> DriveToReadyForBillingAsync(Guid customerId, decimal itemPrice = 100m)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();

        var wo = new WorkOrder(customerId, $"Test WO {Guid.NewGuid():N}");
        wo.Assign(Guid.NewGuid());
        wo.MarkAsEnRoute();
        wo.CompleteSafetyChecklist();
        wo.Start(null);
        wo.CheckIn(null);
        wo.CheckOut(null);
        wo.AddItem("Part A", 1, itemPrice);
        wo.Complete("SIG", null);
        wo.ApproveForBilling();
        db.WorkOrders.Add(wo);
        await db.SaveChangesAsync();
        return wo;
    }

    private System.Net.Http.HttpRequestMessage AuthPost(string url, string token, object? body = null)
    {
        var req = new System.Net.Http.HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (body is not null) req.Content = System.Net.Http.Json.JsonContent.Create(body);
        return req;
    }

    private System.Net.Http.HttpRequestMessage AuthGet(string url, string token)
    {
        var req = new System.Net.Http.HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    // ─── happy path: record payment ─────────────────────────────────────────

    [Fact]
    [Trait("VUT", "05201")]
    public async Task HappyPath_CreatePayment_ReturnsPaymentDto()
    {
        var (token, customer) = await SetupAsync();

        using var req = AuthPost("/api/payments", token, new
        {
            customerId = customer.Id,
            amount = 250m,
            paymentMethod = "Cash",
            paymentDate = "2026-01-15"
        });
        using var resp = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.NotNull(dto);
        Assert.Equal(customer.Id, dto!.CustomerId);
        Assert.Equal(250m, dto.Amount);
    }

    // ─── happy path: invoice work order creates SalesInvoice (H7) ───────────

    [Fact]
    [Trait("VUT", "05101")]
    public async Task InvoiceWorkOrder_CreatesLinkedSalesInvoice()
    {
        var (token, customer) = await SetupAsync();
        var wo = await DriveToReadyForBillingAsync(customer.Id, itemPrice: 500m);

        using var invoiceReq = AuthPost($"/api/workorders/{wo.Id}/invoice", token);
        using var invoiceResp = await _client.SendAsync(invoiceReq);
        Assert.Equal(HttpStatusCode.OK, invoiceResp.StatusCode);
        var woDto = await invoiceResp.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.Equal("Invoiced", woDto!.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var invoice = db.SalesInvoices.FirstOrDefault(i => i.CustomerId == customer.Id);
        Assert.NotNull(invoice);
        Assert.Equal(500m, invoice!.GrandTotal);
    }

    // ─── happy path: list invoices by customer ──────────────────────────────

    [Fact]
    [Trait("VUT", "05101")]
    public async Task ListInvoicesByCustomer_AfterInvoicing_ReturnsInvoice()
    {
        var (token, customer) = await SetupAsync();
        var wo = await DriveToReadyForBillingAsync(customer.Id, itemPrice: 300m);

        using var invoiceReq = AuthPost($"/api/workorders/{wo.Id}/invoice", token);
        using var _ = await _client.SendAsync(invoiceReq);

        using var listReq = AuthGet($"/api/payments/invoices/{customer.Id}", token);
        using var listResp = await _client.SendAsync(listReq);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var items = await listResp.Content.ReadFromJsonAsync<List<SalesInvoiceDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, i => i.GrandTotal == 300m);
    }

    // ─── happy path: allocate payment to invoice ────────────────────────────

    [Fact]
    [Trait("VUT", "05301")]
    public async Task HappyPath_AllocatePayment_ReducesRemainingAmount()
    {
        var (token, customer) = await SetupAsync();
        var wo = await DriveToReadyForBillingAsync(customer.Id, itemPrice: 400m);

        using var invoiceReq = AuthPost($"/api/workorders/{wo.Id}/invoice", token);
        using var _ = await _client.SendAsync(invoiceReq);

        using var payReq = AuthPost("/api/payments", token, new
        {
            customerId = customer.Id,
            amount = 150m,
            paymentMethod = "Card",
            paymentDate = "2026-01-15"
        });
        using var payResp = await _client.SendAsync(payReq);
        var payment = await payResp.Content.ReadFromJsonAsync<PaymentDto>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var invoice = db.SalesInvoices.First(i => i.CustomerId == customer.Id);

        using var allocReq = AuthPost("/api/payments/allocate", token, new
        {
            paymentId = payment!.Id,
            invoiceId = invoice.Id,
            amount = 150m
        });
        using var allocResp = await _client.SendAsync(allocReq);
        Assert.Equal(HttpStatusCode.OK, allocResp.StatusCode);

        db.ChangeTracker.Clear();
        var updated = db.SalesInvoices.Single(i => i.Id == invoice.Id);
        Assert.Equal(150m, updated.PaidAmount);
        Assert.Equal(250m, updated.RemainingAmount);
    }

    // ─── lists of everyone's payments and invoices ──────────────────────────

    [Fact]
    [Trait("VUT", "05201")]
    public async Task ListPayments_AcrossCustomers_CarriesTheNames_AndCanBeNarrowedToOneCustomer()
    {
        var (token, first) = await SetupAsync();
        Customer second;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
            second = new Customer($"Pay Cust {Guid.NewGuid():N}", $"pay-cust-{Guid.NewGuid():N}@test.com", "5550003");
            db.Add(second);
            await db.SaveChangesAsync();
        }

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        foreach (var (customer, amount) in new[] { (first, 111m), (second, 222m) })
        {
            using var pay = AuthPost("/api/payments", token, new { customerId = customer.Id, amount, paymentMethod = "Cash", paymentDate = today });
            using var payResp = await _client.SendAsync(pay);
            Assert.Equal(HttpStatusCode.OK, payResp.StatusCode);
        }

        using var allReq = AuthGet("/api/payments?limit=100", token);
        using var all = await _client.SendAsync(allReq);
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        Assert.True(all.Headers.Contains("X-Total-Count"));
        var everyone = await all.Content.ReadFromJsonAsync<List<PaymentSummaryDto>>();
        Assert.Contains(everyone!, p => p.CustomerId == first.Id && p.CustomerName == first.FullName && p.Amount == 111m);
        Assert.Contains(everyone!, p => p.CustomerId == second.Id && p.CustomerName == second.FullName && p.Amount == 222m);

        using var oneReq = AuthGet($"/api/payments?customerId={first.Id}", token);
        using var one = await _client.SendAsync(oneReq);
        Assert.Equal("1", one.Headers.GetValues("X-Total-Count").Single());
        var only = await one.Content.ReadFromJsonAsync<List<PaymentSummaryDto>>();
        var row = Assert.Single(only!);
        Assert.Equal(first.Id, row.CustomerId);
        Assert.Equal(111m, row.Amount);
    }

    [Fact]
    [Trait("VUT", "05101")]
    public async Task ListInvoices_AcrossCustomers_CarriesTheNames_AndCanBeNarrowedToOneCustomer()
    {
        var (token, customer) = await SetupAsync();
        var wo = await DriveToReadyForBillingAsync(customer.Id, itemPrice: 700m);
        using var invoiceReq = AuthPost($"/api/workorders/{wo.Id}/invoice", token);
        using var invoiced = await _client.SendAsync(invoiceReq);
        Assert.Equal(HttpStatusCode.OK, invoiced.StatusCode);

        using var allReq = AuthGet("/api/payments/invoices?limit=100", token);
        using var all = await _client.SendAsync(allReq);
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        Assert.True(all.Headers.Contains("X-Total-Count"));
        var everyone = await all.Content.ReadFromJsonAsync<List<SalesInvoiceSummaryDto>>();
        Assert.Contains(everyone!, i => i.CustomerId == customer.Id && i.CustomerName == customer.FullName && i.GrandTotal == 700m);

        using var oneReq = AuthGet($"/api/payments/invoices?customerId={customer.Id}", token);
        using var one = await _client.SendAsync(oneReq);
        Assert.Equal("1", one.Headers.GetValues("X-Total-Count").Single());
        var only = await one.Content.ReadFromJsonAsync<List<SalesInvoiceSummaryDto>>();
        var row = Assert.Single(only!);
        Assert.Equal(700m, row.RemainingAmount);
    }

    [Theory]
    [InlineData("/api/payments")]
    [InlineData("/api/payments/invoices")]
    public async Task ListingEveryonesMoney_WithoutASignIn_ShouldReturn401(string url)
    {
        using var resp = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ─── error paths ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePayment_ZeroAmount_ShouldReturn422()
    {
        var (token, customer) = await SetupAsync();

        using var req = AuthPost("/api/payments", token, new
        {
            customerId = customer.Id,
            amount = 0m,
            paymentMethod = "Cash",
            paymentDate = "2026-01-15"
        });
        using var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_UnknownCustomer_ShouldReturn422()
    {
        var (token, _) = await SetupAsync();

        using var req = AuthPost("/api/payments", token, new
        {
            customerId = Guid.NewGuid(),
            amount = 100m,
            paymentMethod = "Cash",
            paymentDate = "2026-01-15"
        });
        using var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task AllocatePayment_ExceedsUnallocated_ShouldReturn422()
    {
        var (token, customer) = await SetupAsync();
        var wo = await DriveToReadyForBillingAsync(customer.Id, itemPrice: 100m);

        using var invoiceReq = AuthPost($"/api/workorders/{wo.Id}/invoice", token);
        using var _ = await _client.SendAsync(invoiceReq);

        using var payReq = AuthPost("/api/payments", token, new
        {
            customerId = customer.Id,
            amount = 50m,
            paymentMethod = "Cash",
            paymentDate = "2026-01-15"
        });
        using var payResp = await _client.SendAsync(payReq);
        var payment = await payResp.Content.ReadFromJsonAsync<PaymentDto>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var invoice = db.SalesInvoices.First(i => i.CustomerId == customer.Id);

        using var allocReq = AuthPost("/api/payments/allocate", token, new
        {
            paymentId = payment!.Id,
            invoiceId = invoice.Id,
            amount = 200m
        });
        using var allocResp = await _client.SendAsync(allocReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, allocResp.StatusCode);
    }

    [Fact]
    public async Task AllocatePayment_SameAllocationTwice_ShouldReturn422()
    {
        var (token, customer) = await SetupAsync();
        var wo = await DriveToReadyForBillingAsync(customer.Id, itemPrice: 200m);

        using var invoiceReq = AuthPost($"/api/workorders/{wo.Id}/invoice", token);
        using var _ = await _client.SendAsync(invoiceReq);

        using var payReq = AuthPost("/api/payments", token, new
        {
            customerId = customer.Id,
            amount = 100m,
            paymentMethod = "Cash",
            paymentDate = "2026-01-15"
        });
        using var payResp = await _client.SendAsync(payReq);
        var payment = await payResp.Content.ReadFromJsonAsync<PaymentDto>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var invoice = db.SalesInvoices.First(i => i.CustomerId == customer.Id);

        using var alloc1 = AuthPost("/api/payments/allocate", token, new { paymentId = payment!.Id, invoiceId = invoice.Id, amount = 50m });
        using var resp1 = await _client.SendAsync(alloc1);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        using var alloc2 = AuthPost("/api/payments/allocate", token, new { paymentId = payment.Id, invoiceId = invoice.Id, amount = 50m });
        alloc2.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var resp2 = await _client.SendAsync(alloc2);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp2.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_ShouldReturn401()
    {
        using var resp = await _client.GetAsync("/api/payments/invoices/" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
