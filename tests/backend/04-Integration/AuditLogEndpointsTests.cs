using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Identity;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Tests;

// Guards the authorization policies the audit-log endpoints ask for. A policy name that is not registered makes
// every request fail at runtime (the API answers with an error, not a build failure), which no unit test notices.
[Xunit.Collection("ApiIntegration")]
public sealed class AuditLogEndpointsTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _factory;

    public AuditLogEndpointsTests(ApiTestFixture factory) => _factory = factory;

    private async Task<HttpClient> ClientWithRoleAsync(string role)
    {
        var unique = Guid.NewGuid().ToString("N");
        await using var scope = _factory.Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var user = new AppUser($"{role} {unique}", $"{role.ToLowerInvariant()}-{unique}@example.com");
        var token = tokenService.CreateToken(user, [role]);
        dbContext.UserSessions.Add(new UserSession(user.Id, token, DateTime.UtcNow.AddHours(1)));
        await dbContext.SaveChangesAsync();

        var client = _factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task RecentLogs_AsAdmin_ReturnsOk()
    {
        using var client = await ClientWithRoleAsync("Admin");

        using var response = await client.GetAsync("/api/audit-logs/recent");

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RecentLogs_AsViewer_IsForbidden()
    {
        using var client = await ClientWithRoleAsync("Viewer");

        using var response = await client.GetAsync("/api/audit-logs/recent");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecentLogs_WithoutToken_IsUnauthorized()
    {
        using var client = _factory.CreateApiClient();

        using var response = await client.GetAsync("/api/audit-logs/recent");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
