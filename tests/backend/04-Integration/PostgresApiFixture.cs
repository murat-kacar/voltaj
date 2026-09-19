using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Voltflow.Application.Interfaces;

namespace Voltflow.Tests;

/// <summary>
/// A test that needs a real PostgreSQL. The in-memory provider used by most tests cannot tell whether a query translates to
/// SQL or whether the migrations build the schema, and the bugs it hides are exactly those. It runs only when
/// <see cref="EnvironmentVariable"/> holds a connection string to any database on a server (CI provides one).
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "VOLTFLOW_TEST_POSTGRES";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable)))
            Skip = $"Set {EnvironmentVariable} to a PostgreSQL connection string to run the tests against a real database.";
    }
}

/// <summary>The API on its own, freshly migrated PostgreSQL database, which is dropped when the fixture goes away.</summary>
public sealed class PostgresApiFixture : WebApplicationFactory<Program>
{
    private readonly string _serverConnectionString = Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvironmentVariable) ?? string.Empty;
    private readonly string _database = $"voltflow_test_{Guid.NewGuid():N}";

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_serverConnectionString);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_serverConnectionString) { Database = _database }.ConnectionString;

        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:Key", "integration-test-signing-key-32-chars");
        builder.UseSetting("Jwt:Issuer", "Voltflow");
        builder.UseSetting("Jwt:Audience", "Voltflow.Api");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("Database:Provider", "PostgreSql");
        // starting the API migrates the empty database from zero, exactly as a fresh environment does
        builder.UseSetting("Database:SeedOnStartup", "true");
        builder.ConfigureServices(services =>
        {
            Replace<IDistributedRateLimiter, ApiTestFixture.FakeRateLimiter>(services);
            Replace<ISessionCacheService, ApiTestFixture.FakeSessionCacheService>(services);
        });
    }

    private static void Replace<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        var existing = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(TService));
        if (existing is not null) services.Remove(existing);
        services.AddSingleton<TService, TImplementation>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || !IsConfigured) return;

        // pooled connections would keep the database busy
        NpgsqlConnection.ClearAllPools();
        using var connection = new NpgsqlConnection(_serverConnectionString);
        connection.Open();
        using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", connection);
        command.ExecuteNonQuery();
    }
}
