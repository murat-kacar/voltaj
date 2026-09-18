using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Voltflow.Domain.Identity;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Infrastructure.Seed;
using Xunit;

namespace Voltflow.Tests.Infrastructure;

// No default administrator outside Development; the seed never revives a disabled account.
public class SeedDataTests
{
    private const string DevelopmentAdminEmail = "admin@voltflow.com";

    private static VoltflowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VoltflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new VoltflowDbContext(options);
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(x => x.Key, x => (string?)x.Value))
            .Build();

    private static IHostEnvironment Environment(string name)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(x => x.EnvironmentName).Returns(name);
        return environment.Object;
    }

    [Fact]
    public async Task Production_WithoutAdminConfiguration_SeedsNoAdministrator_ButStillSeedsRoles()
    {
        await using var db = CreateContext();
        var logger = new CapturingLogger();

        await SeedData.ApplyAsync(db, Config(), Environment(Environments.Production), logger);

        (await db.AppUsers.CountAsync()).Should().Be(0);
        (await db.AppRoles.CountAsync()).Should().Be(4);
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Message.Contains("No administrator was seeded"));
    }

    [Theory]
    [InlineData("Seed:AdminEmail")]
    [InlineData("Seed:AdminPassword")]
    public async Task Production_WithOnlyOneOfEmailAndPassword_SeedsNoAdministrator(string configuredKey)
    {
        await using var db = CreateContext();

        await SeedData.ApplyAsync(db, Config((configuredKey, "owner@example.com")), Environment(Environments.Production));

        (await db.AppUsers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Production_WithExplicitAdministrator_SeedsThatAdministratorAndNoDefaultOne()
    {
        await using var db = CreateContext();
        var password = Guid.NewGuid().ToString("N") + "Aa1!";

        await SeedData.ApplyAsync(
            db,
            Config(("Seed:AdminEmail", "  Owner@Example.COM "), ("Seed:AdminPassword", password)),
            Environment(Environments.Production));

        var users = await db.AppUsers.ToListAsync();
        users.Should().ContainSingle();
        var owner = users.Single();
        owner.Email.Should().Be("owner@example.com");
        owner.IsApproved.Should().BeTrue();
        new PasswordHasher<AppUser>().VerifyHashedPassword(owner, owner.PasswordHash, password)
            .Should().NotBe(PasswordVerificationResult.Failed);
        var adminRoleId = await db.AppRoles.Where(r => r.Name == "Admin").Select(r => r.Id).SingleAsync();
        (await db.AppUserRoles.AnyAsync(x => x.UserId == owner.Id && x.RoleId == adminRoleId)).Should().BeTrue();
        users.Should().NotContain(u => u.Email == DevelopmentAdminEmail);
    }

    [Fact]
    public async Task Development_WithoutConfiguration_SeedsTheDevelopmentAdministrator()
    {
        await using var db = CreateContext();

        await SeedData.ApplyAsync(db, Config(), Environment(Environments.Development));

        var admin = await db.AppUsers.SingleAsync(u => u.Email == DevelopmentAdminEmail);
        admin.IsApproved.Should().BeTrue();
        var adminRoleId = await db.AppRoles.Where(r => r.Name == "Admin").Select(r => r.Id).SingleAsync();
        (await db.AppUserRoles.AnyAsync(x => x.UserId == admin.Id && x.RoleId == adminRoleId)).Should().BeTrue();
    }

    [Fact]
    public async Task Production_NeverRevivesAnAccountThatWasDisabled()
    {
        // The live server's default administrator is disabled by hand; every restart runs the seed again.
        await using var db = CreateContext();
        await SeedData.ApplyAsync(db, Config(), Environment(Environments.Development));
        var admin = await db.AppUsers.SingleAsync(u => u.Email == DevelopmentAdminEmail);
        db.Entry(admin).Property(x => x.IsApproved).CurrentValue = false;
        await db.SaveChangesAsync();

        await SeedData.ApplyAsync(db, Config(), Environment(Environments.Production));

        (await db.AppUsers.SingleAsync(u => u.Email == DevelopmentAdminEmail)).IsApproved.Should().BeFalse();
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
