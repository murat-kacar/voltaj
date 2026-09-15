using Voltflow.Application.Interfaces;

namespace Voltflow.Worker;

public sealed class OutboxProcessor
{
    private readonly IOutboxRepository _repository;
    private readonly IOutboxPublisher _publisher;

    public OutboxProcessor(IOutboxRepository repository, IOutboxPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<int> ProcessDueAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var messages = await _repository.ListDueAsync(utcNow, 100, ct);
        foreach (var message in messages)
        {
            try
            {
                await _publisher.PublishAsync(message, ct);
                await _repository.MarkProcessedAsync(message.Id, ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await _repository.MarkFailedAsync(message.Id, exception.Message, ct);
            }

        }

        return messages.Count;
    }
}

public sealed class AuditOutboxPublisher : IOutboxPublisher
{
    public Task PublishAsync(OutboxWorkItem message, CancellationToken ct = default)
    {
        if (message.EventType == "AuditEventRecorded" || message.EventType == "WorkOrderCompleted" || message.EventType == "TechnicianEnRoute")
        {
            // For now, we simulate processing by doing nothing or logging it via an injected logger if we had one.
            // In a real scenario, this would send an email or SMS notification.
            return Task.CompletedTask;
        }

        throw new InvalidOperationException($"No publisher is registered for outbox event '{message.EventType}'.");
    }
}
