using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static Voltflow.Tests.TestApi;

namespace Voltflow.Tests;

// What a client gets back when the request itself is broken, as opposed to refused for a business reason: a 4xx that says
// so, in the same problem format as every other answer, and never a 500 that looks like the server's own fault.
[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "08301")]
public sealed class ErrorHandlingIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _factory;

    public ErrorHandlingIntegrationTests(ApiTestFixture factory) => _factory = factory;

    public static TheoryData<string, byte[]> UnreadableBodies => new()
    {
        { "truncated JSON", Utf8("{\"name\":\"Home\",\"address\":") },
        { "text that is not JSON", Utf8("this is not json") },
        { "values of the wrong types", Utf8("{\"name\":{\"nested\":true},\"address\":12}") },
        { "bytes that are not UTF-8", [.. Utf8("{\"name\":\""), 0xC3, 0x28, .. Utf8("\"}")] },
        { "no body at all", [] }
    };

    [Theory]
    [MemberData(nameof(UnreadableBodies))]
    public async Task ABodyThatCannotBeRead_IsRefusedWith400_AndNotReportedAsAServerFailure(string what, byte[] body)
    {
        using var manager = await _factory.ActorAsync("Manager");

        // the body is read before anything else about the request is looked at, so the customer need not exist
        using var response = await PostRawAsync(manager.Client, $"/api/customers/{Guid.NewGuid()}/sites", body, "application/json");

        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{what}: expected 400 but got {(int)response.StatusCode}: {text}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(text);
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("https://voltflow.dev/problems/invalid_request", problem.RootElement.GetProperty("type").GetString());
        Assert.Equal("The request is not valid.", problem.RootElement.GetProperty("detail").GetString());

        // what the framework said about the body (type names, parser positions) is for the log, not for the client
        foreach (var inside in new[] { "SaveSiteRequest", "System.Text.Json", "LineNumber", "BytePositionInLine", "Failed to read" })
            Assert.DoesNotContain(inside, text, StringComparison.OrdinalIgnoreCase);
    }

    // the framework raises the same exception when a value of the query cannot be read, which is why the answer does not speak of a body
    [Fact]
    public async Task AQueryValueOfTheWrongType_IsRefusedWith400_AndNotReportedAsAServerFailure()
    {
        using var manager = await _factory.ActorAsync("Manager");

        using var response = await manager.Client.GetAsync("/api/customers?limit=abc");

        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"expected 400 but got {(int)response.StatusCode}: {text}");
        using var problem = JsonDocument.Parse(text);
        Assert.Equal("https://voltflow.dev/problems/invalid_request", problem.RootElement.GetProperty("type").GetString());
        Assert.DoesNotContain("Nullable", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ABodyThatCanBeRead_IsHandedOnToTheModule()
    {
        using var manager = await _factory.ActorAsync("Manager");

        // the module's own refusal of an unknown customer shows that the request got past the reading of its body
        using var unknownCustomer = await PostAsync(manager.Client, $"/api/customers/{Guid.NewGuid()}/sites", new { name = "Home", address = "1 Main Street" });

        await AssertRejectedAsync(unknownCustomer, "CUSTOMER_NOT_FOUND");
    }

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    // every request carries its own idempotency key, as the screen does
    private static async Task<HttpResponseMessage> PostRawAsync(HttpClient client, string url, byte[] body, string contentType)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new ByteArrayContent(body) };
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return await client.SendAsync(request);
    }
}
