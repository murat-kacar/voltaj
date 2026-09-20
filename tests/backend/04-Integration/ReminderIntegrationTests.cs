using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Identity;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Tests;

[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "07101")]
public sealed class ReminderIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public ReminderIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<string> SetupTokenAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var admin = new AppUser($"Rem Admin {Guid.NewGuid():N}", $"rem-admin-{Guid.NewGuid():N}@test.com");
        var token = ts.CreateToken(admin, ["Admin"]);
        db.UserSessions.Add(new UserSession(admin.Id, token, DateTime.UtcNow.AddHours(1)));
        db.Add(admin);
        await db.SaveChangesAsync();
        return token;
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

    // ─── happy path: create ─────────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "07101")]
    public async Task HappyPath_CreateReminder_ReturnsDto()
    {
        var token = await SetupTokenAsync();

        using var req = AuthPost("/api/reminders", token, new
        {
            type = "OverdueWorkOrder",
            entityName = "WorkOrder",
            entityId = Guid.NewGuid(),
            dueAt = DateTime.UtcNow.AddHours(1).ToString("o"),
            message = "Work order is overdue."
        });
        using var resp = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<ReminderDto>();
        Assert.NotNull(dto);
        Assert.Equal("OverdueWorkOrder", dto!.Type);
        Assert.Equal("Pending", dto.State);
        Assert.Equal(0, dto.Attempts);
    }

    // ─── happy path: list ───────────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "07101")]
    public async Task HappyPath_ListReminders_IncludesCreated()
    {
        var token = await SetupTokenAsync();

        using var createReq = AuthPost("/api/reminders", token, new
        {
            type = "ListTest",
            entityName = "Quote",
            entityId = Guid.NewGuid(),
            dueAt = DateTime.UtcNow.AddHours(2).ToString("o"),
            message = "List test reminder."
        });
        using var _ = await _client.SendAsync(createReq);

        using var listReq = AuthGet("/api/reminders?state=Pending", token);
        using var listResp = await _client.SendAsync(listReq);

        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var items = await listResp.Content.ReadFromJsonAsync<List<ReminderDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, r => r.Type == "ListTest");
    }

    // ─── happy path: dismiss ────────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "07101")]
    public async Task HappyPath_DismissReminder_TransitionsToDismissed()
    {
        var token = await SetupTokenAsync();

        using var createReq = AuthPost("/api/reminders", token, new
        {
            type = "DismissTest",
            entityName = "WorkOrder",
            entityId = Guid.NewGuid(),
            dueAt = DateTime.UtcNow.AddHours(1).ToString("o"),
            message = "Dismiss this."
        });
        using var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReminderDto>();

        using var dismissReq = AuthPost($"/api/reminders/{created!.Id}/dismiss", token, new { note = "No longer needed." });
        using var dismissResp = await _client.SendAsync(dismissReq);

        Assert.Equal(HttpStatusCode.OK, dismissResp.StatusCode);
        var dto = await dismissResp.Content.ReadFromJsonAsync<ReminderDto>();
        Assert.NotNull(dto);
        Assert.Equal("Dismissed", dto!.State);
        Assert.Equal("No longer needed.", dto.CompletionNote);
    }

    // ─── happy path: complete ───────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "07101")]
    public async Task HappyPath_CompleteReminder_TransitionsToCompleted()
    {
        var token = await SetupTokenAsync();

        using var createReq = AuthPost("/api/reminders", token, new
        {
            type = "CompleteTest",
            entityName = "WorkOrder",
            entityId = Guid.NewGuid(),
            dueAt = DateTime.UtcNow.AddHours(1).ToString("o"),
            message = "Complete this."
        });
        using var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReminderDto>();

        using var completeReq = AuthPost($"/api/reminders/{created!.Id}/complete", token, new { note = "Done." });
        using var completeResp = await _client.SendAsync(completeReq);

        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
        var dto = await completeResp.Content.ReadFromJsonAsync<ReminderDto>();
        Assert.NotNull(dto);
        Assert.Equal("Completed", dto!.State);
        Assert.Equal("Done.", dto.CompletionNote);
    }

    // ─── error: complete already dismissed ──────────────────────────────────
    // Uses a different action endpoint on the same id so the idempotency guard
    // does not replay the first response (H4 key differs by URL).

    [Fact]
    public async Task Complete_AlreadyDismissed_ShouldReturn422()
    {
        var token = await SetupTokenAsync();

        using var createReq = AuthPost("/api/reminders", token, new
        {
            type = "CompleteAfterDismiss",
            entityName = "WorkOrder",
            entityId = Guid.NewGuid(),
            dueAt = DateTime.UtcNow.AddHours(1).ToString("o"),
            message = "Complete-after-dismiss test."
        });
        using var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReminderDto>();

        // First action: dismiss — succeeds (200)
        using var dismissReq = AuthPost($"/api/reminders/{created!.Id}/dismiss", token, new { });
        using var dismissResp = await _client.SendAsync(dismissReq);
        Assert.Equal(HttpStatusCode.OK, dismissResp.StatusCode);

        // Second action: complete the same (now-dismissed) reminder — different URL → different idempotency key → hits domain guard
        using var completeReq = AuthPost($"/api/reminders/{created.Id}/complete", token, new { });
        using var resp = await _client.SendAsync(completeReq);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    // ─── error: missing required fields ─────────────────────────────────────

    [Fact]
    public async Task Create_EmptyType_ShouldReturn422()
    {
        var token = await SetupTokenAsync();

        using var req = AuthPost("/api/reminders", token, new
        {
            type = "",
            entityName = "WorkOrder",
            entityId = Guid.NewGuid(),
            dueAt = DateTime.UtcNow.AddHours(1).ToString("o"),
            message = "Test."
        });
        using var resp = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    // ─── error: not found ───────────────────────────────────────────────────

    [Fact]
    public async Task Dismiss_UnknownId_ShouldReturn422()
    {
        var token = await SetupTokenAsync();

        using var req = AuthPost($"/api/reminders/{Guid.NewGuid()}/dismiss", token, new { });
        using var resp = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    // ─── auth guard ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Unauthenticated_ShouldReturn401()
    {
        using var resp = await _client.GetAsync("/api/reminders");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
