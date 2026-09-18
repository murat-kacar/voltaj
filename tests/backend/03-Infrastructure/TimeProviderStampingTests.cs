using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Voltflow.Domain.Customers;
using Voltflow.Infrastructure.Persistence;
using Xunit;

namespace Voltflow.Tests.Infrastructure;

public class TimeProviderStampingTests
{
    private static VoltflowDbContext CreateContext(FakeTimeProvider timeProvider)
    {
        var options = new DbContextOptionsBuilder<VoltflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new VoltflowDbContext(options, currentUser: null, operationContext: null, timeProvider: timeProvider);
    }

    [Fact]
    public async Task CreatedAt_ShouldUseInjectedTimeProvider_NotSystemClock()
    {
        var fakeTime = new FakeTimeProvider();
        var fixedNow = new DateTimeOffset(2030, 3, 15, 8, 0, 0, TimeSpan.Zero);
        fakeTime.SetUtcNow(fixedNow);
        await using var dbContext = CreateContext(fakeTime);

        var customer = new Customer("Test Customer", "test@example.com", "555-0000", "TAX-0");
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        customer.CreatedAt.Should().Be(fixedNow.UtcDateTime);
        customer.CreatedAt.Should().NotBeCloseTo(DateTime.UtcNow, TimeSpan.FromDays(1));
    }

    [Fact]
    public async Task UpdatedAt_ShouldAdvanceWithInjectedTimeProvider_OnModification()
    {
        var fakeTime = new FakeTimeProvider();
        var createdAt = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        fakeTime.SetUtcNow(createdAt);
        await using var dbContext = CreateContext(fakeTime);

        var customer = new Customer("Test Customer", "test@example.com", "555-0000", "TAX-0");
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        var updatedAt = createdAt.AddDays(10);
        fakeTime.SetUtcNow(updatedAt);
        customer.ConvertToActive();
        dbContext.Customers.Update(customer);
        await dbContext.SaveChangesAsync();

        customer.UpdatedAt.Should().Be(updatedAt.UtcDateTime);
    }
}
