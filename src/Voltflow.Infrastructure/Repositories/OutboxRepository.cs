using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly VoltflowDbContext _dbContext;

    public OutboxRepository(VoltflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OutboxWorkItem>> ListDueAsync(DateTime utcNow, int batchSize, CancellationToken ct = default)
        => await _dbContext.OutboxMessages
            .Where(x => x.State == OutboxState.Pending && x.NextAttemptAt <= utcNow)
            .OrderBy(x => x.OccurredAt)
            .Take(batchSize)
            .Select(x => new OutboxWorkItem(x.Id, x.EventType, x.PayloadJson, x.Attempts, x.TraceParent, x.TraceState))
            .ToListAsync(ct);

    public async Task AddAsync(OutboxWorkItem message, CancellationToken ct = default)
    {
        var outboxMessage = new OutboxMessage(message.EventType, message.PayloadJson);
        _dbContext.OutboxMessages.Add(outboxMessage);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task QueueAsync(OutboxWorkItem message, CancellationToken ct = default)
    {
        var outboxMessage = new OutboxMessage(message.EventType, message.PayloadJson);
        _dbContext.OutboxMessages.Add(outboxMessage);
        return Task.CompletedTask;
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
    {
        var message = await _dbContext.OutboxMessages.SingleAsync(x => x.Id == id, ct);
        message.MarkAttempt();
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default)
    {
        var message = await _dbContext.OutboxMessages.SingleAsync(x => x.Id == id, ct);
        message.MarkAttempt(error);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task RequeueAsync(Guid id, CancellationToken ct = default)
    {
        var message = await _dbContext.OutboxMessages.SingleAsync(x => x.Id == id, ct);
        message.Requeue();
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OutboxWorkItem>> ListDeadLetterAsync(CancellationToken ct = default)
        => await _dbContext.OutboxMessages
            .Where(x => x.State == OutboxState.DeadLetter)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new OutboxWorkItem(x.Id, x.EventType, x.PayloadJson, x.Attempts, x.TraceParent, x.TraceState))
            .ToListAsync(ct);
}
