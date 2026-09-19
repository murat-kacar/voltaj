namespace Voltflow.Tests;

public static class TestAuthHelper
{
    public static Task<string> GetValidTokenAsync(HttpClient client, string email, string role)
    {
        // Placeholder for a real token generation logic.
        // In a real E2E test, we'd either call /api/auth/login or generate a token manually.
        // Since we are validating the architecture, returning a dummy token works for now,
        // assuming we have a mock Auth handler or bypass in our ApiTestFixture.
        return Task.FromResult("dummy-token-for-" + role);
    }
}
