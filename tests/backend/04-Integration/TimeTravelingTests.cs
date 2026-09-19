using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Worker;
using Voltflow.Domain.WorkOrders;
using Xunit;

namespace Voltflow.Tests;

public class TimeTravelingTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public TimeTravelingTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MaintenanceWorker_ShouldGenerateWorkOrder_WhenTimeTravels6Months()
    {
        // 1. Arrange: FakeTimeProvider set to a fixed date
        var fakeTime = _fixture.FakeTimeProvider;
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        fakeTime.SetUtcNow(startDate);

        using var scope = _fixture.Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<MaintenanceProcessor>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Voltflow.Infrastructure.Persistence.VoltflowDbContext>();

        // Note: For a true E2E, we'd insert a MaintenanceContract into DB here.
        // Assuming we mock the processor or insert data into InMemory DB.
        
        // Let's assume we inserted a contract due in 6 months (July 1st, 2026).
        var contract = new MaintenanceContract(
            Guid.NewGuid(), 
            "Test Contract", 
            6, // FrequencyMonths
            startDate.UtcDateTime.AddMonths(6)
        );
        dbContext.Set<MaintenanceContract>().Add(contract);
        await dbContext.SaveChangesAsync();

        // 2. Act: Run processor immediately (before time travel)
        var processedBefore = await processor.ProcessDueContractsAsync(fakeTime.GetUtcNow().UtcDateTime, CancellationToken.None);
        
        // Assert: No contracts due yet
        processedBefore.Should().Be(0);

        // 3. Act: Time Travel 6 months forward
        fakeTime.Advance(TimeSpan.FromDays(185)); // Advance beyond July 1st

        var processedAfter = await processor.ProcessDueContractsAsync(fakeTime.GetUtcNow().UtcDateTime, CancellationToken.None);

        // 4. Assert: Contract processed
        processedAfter.Should().Be(1);

        // Verify state
        var updatedContract = await dbContext.Set<MaintenanceContract>().FindAsync(contract.Id);
        updatedContract.Should().NotBeNull();
        updatedContract!.NextMaintenanceDate.Should().BeAfter(fakeTime.GetUtcNow().UtcDateTime);
    }
}
