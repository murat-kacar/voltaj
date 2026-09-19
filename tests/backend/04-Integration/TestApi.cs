using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Identity;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Tests;

/// <summary>A signed-in user of a test: the HTTP client that carries their token, and their id.</summary>
public sealed record Actor(HttpClient Client, Guid UserId) : IDisposable
{
    public void Dispose() => Client.Dispose();
}

/// <summary>What the integration tests share for talking to the API as a user who has a role.</summary>
public static class TestApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>A new user with the role, a live session, and a client that is signed in as them.</summary>
    public static async Task<Actor> ActorAsync(this WebApplicationFactory<Program> factory, string role)
    {
        var unique = Guid.NewGuid().ToString("N");
        await using var scope = factory.Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var user = new AppUser($"{role} {unique[..6]}", $"{role.ToLowerInvariant()}-{unique}@example.com");
        var token = tokenService.CreateToken(user, [role]);
        db.AppUsers.Add(user);
        db.UserSessions.Add(new UserSession(user.Id, token, DateTime.UtcNow.AddHours(1)));
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new Actor(client, user.Id);
    }

    public static Task<HttpResponseMessage> PostAsync(HttpClient client, string url, object? body = null) => SendAsync(client, HttpMethod.Post, url, body);

    public static Task<HttpResponseMessage> PutAsync(HttpClient client, string url, object body) => SendAsync(client, HttpMethod.Put, url, body);

    public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, string url) => SendAsync(client, HttpMethod.Delete, url, null);

    // every request carries its own idempotency key, as the screen does; without one, two equal requests would be treated as one
    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body ?? new { }) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return await client.SendAsync(request);
    }

    /// <summary>The body of a successful response; anything but the expected status fails the test with what the API said.</summary>
    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using (response)
        {
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"Expected {(int)expected} but got {(int)response.StatusCode}: {text}");
            return JsonSerializer.Deserialize<T>(text, Json)!;
        }
    }

    /// <summary>The API refused on a business rule: 422 with the given error code.</summary>
    public static async Task AssertRejectedAsync(HttpResponseMessage response, string errorCode)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.UnprocessableEntity, $"Expected 422 but got {(int)response.StatusCode}: {text}");
        Assert.Contains(errorCode, text, StringComparison.Ordinal);
    }
}
