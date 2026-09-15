using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Worker;
using Voltflow.Domain.WorkOrders;
using Voltflow.Infrastructure.Persistence;
using Xunit;

namespace Voltflow.Tests;

public class OutboxDrainTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public OutboxDrainTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CompleteWorkOrder_ShouldGenerateOutboxMessage_AndBeDrainedByWorker()
    {
        // 1. Arrange: Setup DB and add a WorkOrder in InProgress state
        using var scope = _fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        
        var order = new WorkOrder(Guid.NewGuid(), "Test Order for Outbox");
        order.Assign(Guid.NewGuid());
        order.CompleteSafetyChecklist();
        order.Start();
        
        dbContext.WorkOrders.Add(order);
        await dbContext.SaveChangesAsync();

        // 2. Act 1: Manually add an OutboxMessage to simulate a domain event or audit event being queued
        var outboxMessage = new OutboxMessage("AuditEventRecorded", "{}");
        dbContext.OutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync();

        // 3. Assert 1: Outbox message created
        var outboxMessages = dbContext.OutboxMessages.Where(m => m.ProcessedAt == null).ToList();
        outboxMessages.Should().Contain(m => m.EventType == "AuditEventRecorded");

        // 4. Act 2 (Worker Drain)
        var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
        var processedCount = await processor.ProcessDueAsync(DateTime.UtcNow, CancellationToken.None);

        // 5. Assert 2: Drain verified
        processedCount.Should().BeGreaterThan(0);
        
        // Verify DB State
        var processedMessage = await dbContext.OutboxMessages.FindAsync(outboxMessages.First().Id);
        processedMessage.Should().NotBeNull();
        processedMessage!.ProcessedAt.Should().NotBeNull();
        processedMessage.LastError.Should().BeNull();
    }
}
