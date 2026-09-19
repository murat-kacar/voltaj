using System.Net;
using Voltflow.Application.Dtos;
using static Voltflow.Tests.TestApi;

namespace Voltflow.Tests;

// Everyday work by signed-in users must not be throttled at the five requests a minute that guard the open entry points.
[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "08201")]
public sealed class RateLimitScopeTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _factory;

    public RateLimitScopeTests(ApiTestFixture factory) => _factory = factory;

    [Fact]
    public async Task EverydayWork_IsNotStoppedAtTheLimitOfTheOpenEntryPoints()
    {
        using var manager = await _factory.ActorAsync("Manager");

        for (var attempt = 0; attempt < 8; attempt++)
        {
            using var response = await PostAsync(
                manager.Client, "/api/products", new CreateProductRequest($"RL-{Guid.NewGuid():N}"[..14], "Rate limit product", null, null, 1m, 20m, false));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Registration_StaysOnTheTightLimit()
    {
        using var client = _factory.CreateClient();
        HttpStatusCode last = default;

        for (var attempt = 0; attempt < 6; attempt++)
        {
            using var response = await PostAsync(
                client, "/api/auth/register", new RegisterUserRequest("Rate Test", $"rate-{Guid.NewGuid():N}@example.com", "Sup3r-secret-Pass!"));
            last = response.StatusCode;
        }

        Assert.Equal((HttpStatusCode)429, last);
    }
}
