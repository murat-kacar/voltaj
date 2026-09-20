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
