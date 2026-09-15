using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Identity;
using Voltflow.Domain.Reminders;
using Voltflow.Domain.WorkOrders;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Infrastructure.Repositories;
using Voltflow.Worker;

namespace Voltflow.Tests;


[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "01101")]
[Trait("VUT", "01102")]
public sealed class AuthIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public AuthIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    [Trait("VUT", "01303")]
    public async Task ProtectedEndpoint_ShouldReturnProblemDetailsUnauthorized()
    {
        using var response = await _client.GetAsync("/api/customers");

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, responseBody);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unauthorized", body.GetProperty("type").GetString()?.Split('/').Last());
    }

    [Fact]
    [Trait("VUT", "01201")]
    public async Task Register_WithSameIdempotencyKey_ShouldReplayCompletedResponse()
    {
        var request = new RegisterUserRequest(
            $"Integration User {Guid.NewGuid():N}",
            $"integration-{Guid.NewGuid():N}@example.com",
            "StrongPassword123!");
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(request)
        };
        firstRequest.Headers.Add("Idempotency-Key", $"integration-{Guid.NewGuid():N}");

        using var firstResponse = await _client.SendAsync(firstRequest);
        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        if (firstResponse.StatusCode != HttpStatusCode.Accepted)
            throw new Xunit.Sdk.XunitException(firstBody);

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(request)
        };
        replayRequest.Headers.Add("Idempotency-Key", firstRequest.Headers.GetValues("Idempotency-Key").Single());

        using var replayResponse = await _client.SendAsync(replayRequest);
        var replayBody = await replayResponse.Content.ReadAsStringAsync();
        if (replayResponse.StatusCode != HttpStatusCode.Accepted)
            throw new Xunit.Sdk.XunitException(replayBody);
        Assert.Equal(firstBody, replayBody);
    }

    [Fact]
    [Trait("VUT", "08201")]
    public async Task Login_ShouldBeRateLimitedAfterFiveMutationRequests()
    {
        HttpResponseMessage? sixthResponse = null;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            sixthResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
                $"missing-{Guid.NewGuid():N}@example.com", "invalid-password"));
        }

        Assert.NotNull(sixthResponse);
        Assert.Equal((HttpStatusCode)429, sixthResponse!.StatusCode);
    }

    [Fact]
    [Trait("VUT", "01204")]
    public async Task Register_ShouldCreateTraceAuditAndOutboxLineage()
    {
        var request = new RegisterUserRequest(
            $"Lineage User {Guid.NewGuid():N}",
            $"lineage-{Guid.NewGuid():N}@example.com",
            "StrongPassword123!");
        using var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var operationId = Guid.Parse(response.Headers.GetValues("X-Operation-Id").Single());

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        Assert.NotNull(await dbContext.OperationTraces.SingleOrDefaultAsync(x => x.OperationId == operationId));
        var auditEvents = await dbContext.AuditEvents.Where(x => x.OperationId == operationId).ToListAsync();
        Assert.Contains(auditEvents, x => x.EventType == "CREATED");
        Assert.DoesNotContain(request.Email, string.Join("|", auditEvents.Select(x => x.AfterJson)));
        Assert.Contains(await dbContext.OutboxMessages.Where(x => x.PayloadJson.Contains(operationId.ToString())).ToListAsync(), x => x.EventType == "AuditEventRecorded");
    }

    [Fact]
    [Trait("VUT", "01404")]
    public async Task Viewer_ShouldBeForbiddenFromWriteMutation()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var viewer = new AppUser($"Viewer {Guid.NewGuid():N}", $"viewer-{Guid.NewGuid():N}@example.com");
        var token = tokenService.CreateToken(viewer, ["Viewer"]);
        dbContext.UserSessions.Add(new UserSession(viewer.Id, token, DateTime.UtcNow.AddHours(1)));
        await dbContext.SaveChangesAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/customers")
        {
            Content = JsonContent.Create(new Voltflow.Application.Dtos.CreateCustomerRequest(
                "Forbidden candidate", "forbidden@example.com", "5552000", $"VIEWER-{Guid.NewGuid():N}"))
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("VUT", "01403")]
    [Trait("VUT", "01405")]
    public async Task Admin_ShouldApproveAndAssignRoleToPendingUser()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var pendingUser = new AppUser($"Pending {Guid.NewGuid():N}", $"pending-{Guid.NewGuid():N}@example.com");
        dbContext.AppUsers.Add(pendingUser);
        if (!await dbContext.AppRoles.AnyAsync(r => r.Name == "Viewer")) dbContext.AppRoles.Add(new AppRole("Viewer"));
        if (!await dbContext.AppRoles.AnyAsync(r => r.Name == "Technician")) dbContext.AppRoles.Add(new AppRole("Technician"));
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var admin = new AppUser($"Approval Admin {Guid.NewGuid():N}", $"approval-admin-{Guid.NewGuid():N}@example.com");
        var adminToken = tokenService.CreateToken(admin, ["Admin"]);
        dbContext.UserSessions.Add(new UserSession(admin.Id, adminToken, DateTime.UtcNow.AddHours(1)));
        await dbContext.SaveChangesAsync();

        async Task<HttpResponseMessage> PostAsync(string path, object? body = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            if (body is not null) request.Content = JsonContent.Create(body);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            return await _client.SendAsync(request);
        }

        using var approveResponse = await PostAsync($"/api/auth/users/{pendingUser.Id}/approve");
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        using var roleResponse = await PostAsync($"/api/auth/users/{pendingUser.Id}/roles", new AssignRoleRequest("Technician"));
        Assert.Equal(HttpStatusCode.NoContent, roleResponse.StatusCode);

        var persisted = await dbContext.AppUsers.AsNoTracking().SingleAsync(x => x.Id == pendingUser.Id);
        Assert.True(persisted.IsApproved);
        var roles = await dbContext.AppUserRoles.AsNoTracking().Where(x => x.UserId == pendingUser.Id).ToListAsync();
        Assert.Equal(2, roles.Count);
    }

    [Fact]
    [Trait("VUT", "01301")]
    [Trait("VUT", "01302")]
    public async Task SessionRevocation_ShouldInvalidateSessionAndRejectSubsequentCalls()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = new AppUser($"Revoke User {Guid.NewGuid():N}", $"revoke-{Guid.NewGuid():N}@example.com");
        var customer = new Customer("Test Customer", $"cust-{Guid.NewGuid():N}@example.com", "5550001");
        dbContext.AddRange(user, customer);
        await dbContext.SaveChangesAsync();

        var token = tokenService.CreateToken(user, ["Admin"]);
        dbContext.UserSessions.Add(new UserSession(user.Id, token, DateTime.UtcNow.AddHours(1)));
        await dbContext.SaveChangesAsync();

        // 1. Initial request with active token succeeds
        using var initialRequest = new HttpRequestMessage(HttpMethod.Get, "/api/customers");
        initialRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var initialResponse = await _client.SendAsync(initialRequest);
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);

        // 2. Revoke session
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/session/revoke")
        {
            Content = JsonContent.Create(new RevokeSessionRequest(token))
        };
        revokeRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var revokeResponse = await _client.SendAsync(revokeRequest);
        Assert.True(revokeResponse.IsSuccessStatusCode);

        // 3. Subsequent request with the same token must be rejected (401 Unauthorized)
        using var subsequentRequest = new HttpRequestMessage(HttpMethod.Get, "/api/customers");
        subsequentRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var subsequentResponse = await _client.SendAsync(subsequentRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, subsequentResponse.StatusCode);
    }

}
