using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Voltflow.Tests;

public sealed class ApiTestFixture : WebApplicationFactory<Program>
{
    public HttpClient CreateApiClient() => CreateClient();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:Key", "integration-test-signing-key-32-chars");
        builder.UseSetting("Jwt:Issuer", "Voltflow");
        builder.UseSetting("Jwt:Audience", "Voltflow.Api");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-test-signing-key-32-chars",
                ["Jwt:Issuer"] = "Voltflow",
                ["Jwt:Audience"] = "Voltflow.Api",
                ["RateLimiting:PermitLimit"] = "5",
                ["Redis:RateLimitPrefix"] = $"voltflow-test-{Guid.NewGuid():N}",
                ["Database:SeedOnStartup"] = "false",
                ["Database:Provider"] = "InMemory",
                ["Database:Name"] = $"VoltflowTests_{Guid.NewGuid()}"
            }));
        builder.ConfigureServices(services =>
        {
            var rateLimiterDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Voltflow.Application.Interfaces.IDistributedRateLimiter));
            if (rateLimiterDescriptor != null) services.Remove(rateLimiterDescriptor);
            services.AddSingleton<Voltflow.Application.Interfaces.IDistributedRateLimiter, FakeRateLimiter>();

            var sessionCacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Voltflow.Application.Interfaces.ISessionCacheService));
            if (sessionCacheDescriptor != null) services.Remove(sessionCacheDescriptor);
            services.AddSingleton<Voltflow.Application.Interfaces.ISessionCacheService, FakeSessionCacheService>();
            
            var timeProviderDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TimeProvider));
            if (timeProviderDescriptor != null) services.Remove(timeProviderDescriptor);
            services.AddSingleton<TimeProvider>(FakeTimeProvider);
            
            services.AddScoped<Voltflow.Worker.OutboxProcessor>();
            services.AddScoped<Voltflow.Application.Interfaces.IOutboxRepository, Voltflow.Infrastructure.Repositories.OutboxRepository>();
            services.AddScoped<Voltflow.Application.Interfaces.IOutboxPublisher, Voltflow.Worker.AuditOutboxPublisher>();
        });
    }

    public Microsoft.Extensions.Time.Testing.FakeTimeProvider FakeTimeProvider { get; } = new();

    internal sealed class FakeRateLimiter : Voltflow.Application.Interfaces.IDistributedRateLimiter
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _counts = new();

        public Task<Voltflow.Application.Interfaces.RateLimitDecision> CheckAsync(string partition, int permitLimit, TimeSpan window, CancellationToken ct = default)
        {
            var count = _counts.AddOrUpdate(partition, 1, (_, c) => c + 1);
            return Task.FromResult(new Voltflow.Application.Interfaces.RateLimitDecision(count <= permitLimit, 0));
        }
    }

    internal sealed class FakeSessionCacheService : Voltflow.Application.Interfaces.ISessionCacheService
    {
        private readonly HashSet<string> _invalidatedTokens = new();

        public Task<bool> IsSessionValidAsync(string token, CancellationToken ct = default)
            => Task.FromResult(!_invalidatedTokens.Contains(token));

        public Task InvalidateSessionAsync(string token, TimeSpan? blacklistTtl = null, CancellationToken ct = default)
        {
            _invalidatedTokens.Add(token);
            return Task.CompletedTask;
        }
    }
}
