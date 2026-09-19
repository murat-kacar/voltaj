namespace Voltflow.Application.Interfaces;

public interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxWorkItem>> ListDueAsync(DateTime utcNow, int batchSize, CancellationToken ct = default);
    Task AddAsync(OutboxWorkItem message, CancellationToken ct = default);
    Task QueueAsync(OutboxWorkItem message, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default);
    Task RequeueAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxWorkItem>> ListDeadLetterAsync(CancellationToken ct = default);
}

public sealed record OutboxWorkItem(
    Guid Id,
    string EventType,
    string PayloadJson,
    int Attempts,
    string? TraceParent = null,
    string? TraceState = null);
public interface IOutboxPublisher
{
    Task PublishAsync(OutboxWorkItem message, CancellationToken ct = default);
}
