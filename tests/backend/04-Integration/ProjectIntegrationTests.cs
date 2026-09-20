using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Identity;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Tests;

[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "06101")]
public sealed class ProjectIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public ProjectIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<(string token, Customer customer)> SetupAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var admin = new AppUser($"Proj Admin {Guid.NewGuid():N}", $"proj-admin-{Guid.NewGuid():N}@test.com");
        var customer = new Customer($"Proj Cust {Guid.NewGuid():N}", $"proj-cust-{Guid.NewGuid():N}@test.com", "5550003");
        var token = ts.CreateToken(admin, ["Admin"]);
        db.UserSessions.Add(new UserSession(admin.Id, token, DateTime.UtcNow.AddHours(1)));
        db.AddRange(admin, customer);
        await db.SaveChangesAsync();
        return (token, customer);
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

    // ─── happy path: create project ─────────────────────────────────────────

    [Fact]
    [Trait("VUT", "06101")]
    public async Task HappyPath_CreateProject_ReturnsProjectDto()
    {
        var (token, customer) = await SetupAsync();

        using var req = AuthPost("/api/projects", token, new
        {
            customerId = customer.Id,
            name = "Solar Install Block C",
            budget = 75000m
        });
        using var resp = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(dto);
        Assert.Equal(customer.Id, dto!.CustomerId);
        Assert.Equal("Solar Install Block C", dto.Name);
        Assert.Equal(75000m, dto.Budget);
        Assert.StartsWith("PR-", dto.Number, StringComparison.Ordinal);
        Assert.Empty(dto.Phases);
    }

    // ─── happy path: add phase ───────────────────────────────────────────────

    [Fact]
    [Trait("VUT", "06101")]
    public async Task HappyPath_AddPhase_ReturnsUpdatedProjectWithPhase()
    {
        var (token, customer) = await SetupAsync();

        using var createReq = AuthPost("/api/projects", token, new { customerId = customer.Id, name = "Phase Test Project", budget = 50000m });
        using var createResp = await _client.SendAsync(createReq);
        var project = await createResp.Content.ReadFromJsonAsync<ProjectDto>();

        using var phaseReq = AuthPost($"/api/projects/{project!.Id}/phases", token, new
        {
            title = "Foundation work",
            plannedAmount = 20000m
        });
        using var phaseResp = await _client.SendAsync(phaseReq);

        Assert.Equal(HttpStatusCode.OK, phaseResp.StatusCode);
        var updated = await phaseResp.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(updated);
        Assert.Single(updated!.Phases);
        Assert.Equal("Foundation work", updated.Phases.First().Title);
        Assert.Equal(20000m, updated.Phases.First().PlannedAmount);
    }

    // ─── happy path: list projects ───────────────────────────────────────────

    [Fact]
    [Trait("VUT", "06101")]
    public async Task HappyPath_ListProjects_IncludesCreatedProject()
    {
        var (token, customer) = await SetupAsync();

        using var createReq = AuthPost("/api/projects", token, new { customerId = customer.Id, name = "List Test Project", budget = 10000m });
        using var _ = await _client.SendAsync(createReq);

        using var listReq = AuthGet("/api/projects", token);
        using var listResp = await _client.SendAsync(listReq);

        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var items = await listResp.Content.ReadFromJsonAsync<List<ProjectDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, p => p.Name == "List Test Project");
    }

    // ─── happy path: billing entry ───────────────────────────────────────────

    [Fact]
    [Trait("VUT", "06101")]
    public async Task HappyPath_CreateBillingEntry_ReturnsBillingEntryDto()
    {
        var (token, customer) = await SetupAsync();

        using var createReq = AuthPost("/api/projects", token, new { customerId = customer.Id, name = "Billing Test Project", budget = 30000m });
        using var createResp = await _client.SendAsync(createReq);
        var project = await createResp.Content.ReadFromJsonAsync<ProjectDto>();

        using var billReq = AuthPost($"/api/billing/{project!.Id}", token, new
        {
            customerId = customer.Id,
            amount = 10000m
        });
        using var billResp = await _client.SendAsync(billReq);

        Assert.Equal(HttpStatusCode.OK, billResp.StatusCode);
        var entry = await billResp.Content.ReadFromJsonAsync<BillingEntryDto>();
        Assert.NotNull(entry);
        Assert.Equal(project.Id, entry!.ProjectId);
        Assert.Equal(10000m, entry.Amount);
    }

    // ─── happy path: list billing entries ────────────────────────────────────

    [Fact]
    [Trait("VUT", "06101")]
    public async Task HappyPath_ListBillingEntries_ReturnsEntries()
    {
        var (token, customer) = await SetupAsync();

        using var createReq = AuthPost("/api/projects", token, new { customerId = customer.Id, name = "Billing List Project", budget = 30000m });
        using var createResp = await _client.SendAsync(createReq);
        var project = await createResp.Content.ReadFromJsonAsync<ProjectDto>();

        using var billReq = AuthPost($"/api/billing/{project!.Id}", token, new { customerId = customer.Id, amount = 5000m });
        using var _ = await _client.SendAsync(billReq);

        using var listReq = AuthGet($"/api/billing/{project.Id}", token);
        using var listResp = await _client.SendAsync(listReq);

        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var items = await listResp.Content.ReadFromJsonAsync<List<BillingEntryDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, e => e.Amount == 5000m);
    }

    // ─── error paths ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_EmptyName_ShouldReturn422()
    {
        var (token, customer) = await SetupAsync();

        using var req = AuthPost("/api/projects", token, new
        {
            customerId = customer.Id,
            name = "",
            budget = 10000m
        });
        using var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProject_MissingCustomer_ShouldReturn422()
    {
        var (token, _) = await SetupAsync();

        using var req = AuthPost("/api/projects", token, new
        {
            customerId = Guid.Empty,
            name = "Test",
            budget = 10000m
        });
        using var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task CreateBillingEntry_ZeroAmount_ShouldReturn422()
    {
        var (token, customer) = await SetupAsync();

        using var createReq = AuthPost("/api/projects", token, new { customerId = customer.Id, name = "Zero Billing Test", budget = 10000m });
        using var createResp = await _client.SendAsync(createReq);
        var project = await createResp.Content.ReadFromJsonAsync<ProjectDto>();

        using var req = AuthPost($"/api/billing/{project!.Id}", token, new { customerId = customer.Id, amount = 0m });
        using var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_ShouldReturn401()
    {
        using var resp = await _client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
