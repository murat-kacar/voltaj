using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Identity;
using Voltflow.Domain.WorkOrders;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Tests;

[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "03101")]
public sealed class WorkOrderIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public WorkOrderIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<(string token, Customer customer)> SetupAdminAndCustomerAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var admin = new AppUser($"WO Admin {Guid.NewGuid():N}", $"wo-admin-{Guid.NewGuid():N}@test.com");
        var customer = new Customer($"WO Customer {Guid.NewGuid():N}", $"wo-cust-{Guid.NewGuid():N}@test.com", "5550001");
        var token = ts.CreateToken(admin, ["Admin"]);
        db.UserSessions.Add(new UserSession(admin.Id, token, DateTime.UtcNow.AddHours(1)));
        db.AddRange(admin, customer);
        await db.SaveChangesAsync();
        return (token, customer);
    }

    private System.Net.Http.HttpRequestMessage Post(string url, string token, object? body = null)
    {
        var req = new System.Net.Http.HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (body is not null) req.Content = JsonContent.Create(body);
        return req;
    }

    private async Task<WorkOrderDto> PostOkAsync(string url, string token, object? body = null)
    {
        using var resp = await _client.SendAsync(Post(url, token, body));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.NotNull(dto);
        return dto!;
    }

    // ─── happy path ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "03101")]
    public async Task HappyPath_FullLifecycle_ReachesInvoiced()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var tech = new AppUser($"Tech {Guid.NewGuid():N}", $"tech-{Guid.NewGuid():N}@test.com");
        var techToken = ts.CreateToken(tech, ["Technician"]);
        db.UserSessions.Add(new UserSession(tech.Id, techToken, DateTime.UtcNow.AddHours(1)));
        db.Add(tech);
        await db.SaveChangesAsync();

        var wo = await PostOkAsync("/api/workorders", token, new { customerId = customer.Id, title = "Full lifecycle test" });
        Assert.Equal("Open", wo.Status);
        Assert.False(wo.IsSafetyChecklistCompleted);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/assign", token, new { employeeUserId = tech.Id });
        Assert.Equal("Assigned", wo.Status);
        Assert.Equal(tech.Id, wo.AssignedUserId);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/en-route", token);
        Assert.Equal("EnRoute", wo.Status);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/safety-checklist", token);
        Assert.True(wo.IsSafetyChecklistCompleted);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/start", token);
        Assert.Equal("InProgress", wo.Status);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/check-in", techToken);
        Assert.Single(wo.TimeEntries.Where(x => x.CheckOutTime is null));

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/check-out", techToken);
        Assert.All(wo.TimeEntries, e => Assert.NotNull(e.CheckOutTime));

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/items", techToken, new { description = "Cable 10m", quantity = 2, unitPrice = 50 });
        Assert.Single(wo.Items);
        Assert.Equal(100m, wo.Total);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/complete", techToken, new { signatureData = "BASE64SIG", proofOfWorkPhotoUrl = (string?)null });
        Assert.Equal("Completed", wo.Status);
        Assert.Equal("BASE64SIG", wo.SignatureData);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/approve-billing", token);
        Assert.Equal("ReadyForBilling", wo.Status);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/invoice", token);
        Assert.Equal("Invoiced", wo.Status);
    }

    // ─── RBAC ───────────────────────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "03102")]
    public async Task Technician_ShouldOnlySeeAssignedWorkOrders()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var tech = new AppUser($"Tech {Guid.NewGuid():N}", $"tech-scope-{Guid.NewGuid():N}@test.com");
        var otherId = Guid.NewGuid();
        var cust = new Customer($"RBAC Cust {Guid.NewGuid():N}", $"rbac-{Guid.NewGuid():N}@test.com", "5554000");
        var assigned = new WorkOrder(cust.Id, "My order"); assigned.Assign(tech.Id);
        var other = new WorkOrder(cust.Id, "Other order"); other.Assign(otherId);
        var techToken = ts.CreateToken(tech, ["Technician"]);
        db.UserSessions.Add(new UserSession(tech.Id, techToken, DateTime.UtcNow.AddHours(1)));
        db.AddRange(tech, cust, assigned, other);
        await db.SaveChangesAsync();

        using var req = new System.Net.Http.HttpRequestMessage(HttpMethod.Get, "/api/workorders");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", techToken);
        using var resp = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var orders = await resp.Content.ReadFromJsonAsync<List<WorkOrderDto>>();
        Assert.NotNull(orders);
        Assert.Contains(orders, o => o.Id == assigned.Id);
        Assert.DoesNotContain(orders, o => o.Id == other.Id);
    }

    [Fact]
    public async Task Unauthenticated_ShouldReturn401()
    {
        using var resp = await _client.GetAsync("/api/workorders");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ─── FSM gates (VF-03201) ───────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "03201")]
    public async Task Start_WithoutSafetyChecklist_ShouldFail()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var tech = new AppUser($"Tech {Guid.NewGuid():N}", $"tech-nosafe-{Guid.NewGuid():N}@test.com");
        db.Add(tech);
        db.UserSessions.Add(new UserSession(tech.Id, ts.CreateToken(tech, ["Technician"]), DateTime.UtcNow.AddHours(1)));
        await db.SaveChangesAsync();

        var wo = await PostOkAsync("/api/workorders", token, new { customerId = customer.Id, title = "Safety gate test" });
        await PostOkAsync($"/api/workorders/{wo.Id}/assign", token, new { employeeUserId = tech.Id });

        using var startResp = await _client.SendAsync(Post($"/api/workorders/{wo.Id}/start", token));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, startResp.StatusCode);
    }

    // ─── FSM: double check-in blocked (VF-03302) ────────────────────────────

    [Fact]
    [Trait("VUT", "03302")]
    public async Task DoubleCheckIn_ShouldFail()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var tech = new AppUser($"Tech {Guid.NewGuid():N}", $"tech-dbl-{Guid.NewGuid():N}@test.com");
        var techToken = ts.CreateToken(tech, ["Technician"]);
        db.UserSessions.Add(new UserSession(tech.Id, techToken, DateTime.UtcNow.AddHours(1)));
        db.Add(tech);
        await db.SaveChangesAsync();

        var wo = await PostOkAsync("/api/workorders", token, new { customerId = customer.Id, title = "Double check-in test" });
        await PostOkAsync($"/api/workorders/{wo.Id}/assign", token, new { employeeUserId = tech.Id });
        await PostOkAsync($"/api/workorders/{wo.Id}/safety-checklist", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/start", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/check-in", techToken);

        // Use a distinct idempotency key so the execution guard doesn't replay the first check-in's 200.
        // This simulates a second operator attempt (not a retry), which the domain should reject.
        var req2 = new System.Net.Http.HttpRequestMessage(HttpMethod.Post, $"/api/workorders/{wo.Id}/check-in");
        req2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", techToken);
        req2.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var secondCheckIn = await _client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, secondCheckIn.StatusCode);
    }

    // ─── FSM: complete without proof (VF-03401) ─────────────────────────────

    [Fact]
    [Trait("VUT", "03401")]
    public async Task Complete_WithoutProof_ShouldFail()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        var wo = await PostOkAsync("/api/workorders", token, new { customerId = customer.Id, title = "No proof test" });
        await PostOkAsync($"/api/workorders/{wo.Id}/assign", token, new { employeeUserId = Guid.NewGuid() });
        await PostOkAsync($"/api/workorders/{wo.Id}/safety-checklist", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/start", token);

        using var resp = await _client.SendAsync(Post($"/api/workorders/{wo.Id}/complete", token,
            new { signatureData = (string?)null, proofOfWorkPhotoUrl = (string?)null }));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    // ─── Hold / Resume path ─────────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "03301")]
    public async Task HoldAndResume_ShouldWorkAndAutoCheckOutActiveEntry()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var tech = new AppUser($"Tech {Guid.NewGuid():N}", $"tech-hold-{Guid.NewGuid():N}@test.com");
        var techToken = ts.CreateToken(tech, ["Technician"]);
        db.UserSessions.Add(new UserSession(tech.Id, techToken, DateTime.UtcNow.AddHours(1)));
        db.Add(tech);
        await db.SaveChangesAsync();

        var wo = await PostOkAsync("/api/workorders", token, new { customerId = customer.Id, title = "Hold test" });
        await PostOkAsync($"/api/workorders/{wo.Id}/assign", token, new { employeeUserId = tech.Id });
        await PostOkAsync($"/api/workorders/{wo.Id}/safety-checklist", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/start", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/check-in", techToken);

        // Hold while a check-in is active — auto-checkout compensation (H10)
        wo = await PostOkAsync($"/api/workorders/{wo.Id}/hold", token, new { reason = "Missing parts" });
        Assert.Equal("OnHold", wo.Status);
        Assert.Equal("Missing parts", wo.HoldReason);
        Assert.All(wo.TimeEntries, e => Assert.NotNull(e.CheckOutTime));

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/resume", token);
        Assert.Equal("InProgress", wo.Status);
        Assert.Null(wo.HoldReason);
    }

    // ─── No-show path ───────────────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "03103")]
    public async Task NoShow_ShouldTransitionToNoShow()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var tech = new AppUser($"Tech {Guid.NewGuid():N}", $"tech-noshow-{Guid.NewGuid():N}@test.com");
        db.Add(tech);
        db.UserSessions.Add(new UserSession(tech.Id, ts.CreateToken(tech, ["Technician"]), DateTime.UtcNow.AddHours(1)));
        await db.SaveChangesAsync();

        var wo = await PostOkAsync("/api/workorders", token, new { customerId = customer.Id, title = "No-show test" });
        await PostOkAsync($"/api/workorders/{wo.Id}/assign", token, new { employeeUserId = tech.Id });
        await PostOkAsync($"/api/workorders/{wo.Id}/en-route", token);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/no-show", token, new { reason = "Nobody answered" });
        Assert.Equal("NoShow", wo.Status);
    }

    // ─── Cancellation path (H10: compensation — auto-checkout) ──────────────

    [Fact]
    [Trait("VUT", "03504")]
    public async Task Cancel_FromInProgress_WithActiveCheckIn_AutoChecksOut()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var tech = new AppUser($"Tech {Guid.NewGuid():N}", $"tech-cancel-{Guid.NewGuid():N}@test.com");
        var techToken = ts.CreateToken(tech, ["Technician"]);
        db.UserSessions.Add(new UserSession(tech.Id, techToken, DateTime.UtcNow.AddHours(1)));
        db.Add(tech);
        await db.SaveChangesAsync();

        var wo = await PostOkAsync("/api/workorders", token, new { customerId = customer.Id, title = "Cancel compensation test" });
        await PostOkAsync($"/api/workorders/{wo.Id}/assign", token, new { employeeUserId = tech.Id });
        await PostOkAsync($"/api/workorders/{wo.Id}/safety-checklist", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/start", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/check-in", techToken);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/cancel", token, new { reason = "Customer called off" });
        Assert.Equal("Cancelled", wo.Status);
        Assert.Equal("Customer called off", wo.CancellationReason);
        // H10: active check-in auto-closed as compensation
        Assert.All(wo.TimeEntries, e => Assert.NotNull(e.CheckOutTime));
    }

    // ─── Cancel blocked once Completed (VF-03504) ───────────────────────────

    [Fact]
    [Trait("VUT", "03504")]
    public async Task Cancel_AfterCompleted_ShouldFail()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        var wo = await PostOkAsync("/api/workorders", token, new { customerId = customer.Id, title = "Cancel-blocked test" });
        await PostOkAsync($"/api/workorders/{wo.Id}/assign", token, new { employeeUserId = Guid.NewGuid() });
        await PostOkAsync($"/api/workorders/{wo.Id}/safety-checklist", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/start", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/complete", token, new { signatureData = "SIG", proofOfWorkPhotoUrl = (string?)null });

        using var resp = await _client.SendAsync(Post($"/api/workorders/{wo.Id}/cancel", token, new { reason = "Too late" }));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    // ─── Idempotency (H4) ───────────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "03101")]
    public async Task CreateWorkOrder_IdempotencyKey_SameKeyTwice_ReturnsSameResult()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        var idempotencyKey = Guid.NewGuid().ToString();

        async Task<WorkOrderDto> CreateWithKey()
        {
            var req = new System.Net.Http.HttpRequestMessage(HttpMethod.Post, "/api/workorders");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Headers.Add("Idempotency-Key", idempotencyKey);
            req.Content = JsonContent.Create(new { customerId = customer.Id, title = "Idempotency test" });
            using var resp = await _client.SendAsync(req);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            return (await resp.Content.ReadFromJsonAsync<WorkOrderDto>())!;
        }

        var first = await CreateWithKey();
        var second = await CreateWithKey();
        Assert.Equal(first.Id, second.Id);
    }

    // ─── Items on DTO (VF-03401) ────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "03401")]
    public async Task AddMaterial_ShouldAppearInItemsAndTotal()
    {
        var (token, customer) = await SetupAdminAndCustomerAsync();

        var wo = await PostOkAsync("/api/workorders", token, new { customerId = customer.Id, title = "Materials test" });
        await PostOkAsync($"/api/workorders/{wo.Id}/assign", token, new { employeeUserId = Guid.NewGuid() });
        await PostOkAsync($"/api/workorders/{wo.Id}/safety-checklist", token);
        await PostOkAsync($"/api/workorders/{wo.Id}/start", token);

        wo = await PostOkAsync($"/api/workorders/{wo.Id}/items", token, new { description = "Panel bolt", quantity = 4, unitPrice = 15.5m });
        Assert.Single(wo.Items);
        Assert.Equal("Panel bolt", wo.Items[0].Description);
        Assert.Equal(62m, wo.Total);
    }
}
