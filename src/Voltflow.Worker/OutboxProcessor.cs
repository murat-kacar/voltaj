using System.Diagnostics;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Observability;

namespace Voltflow.Worker;

public sealed class OutboxProcessor
{
    private static readonly ActivitySource ActivitySource = new(TelemetryServiceCollectionExtensions.WorkerActivitySourceName);

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
            using var activity = StartConsumerActivity(message);
            try
            {
                await _publisher.PublishAsync(message, ct);
                await _repository.MarkProcessedAsync(message.Id, ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity?.SetTag("error.type", exception.GetType().FullName);
                await _repository.MarkFailedAsync(message.Id, exception.Message, ct);
            }

        }

        return messages.Count;
    }

    /// <summary>T2: a `process` span parented on the trace context the API persisted with the message,
    /// so the trace continues across the API -> Worker boundary. Messaging attribute names follow the
    /// OpenTelemetry messaging semantic conventions.</summary>
    private static Activity? StartConsumerActivity(OutboxWorkItem message)
    {
        var parent = default(ActivityContext);
        if (!string.IsNullOrEmpty(message.TraceParent))
            ActivityContext.TryParse(message.TraceParent, message.TraceState, isRemote: true, out parent);

        var activity = ActivitySource.StartActivity($"process {message.EventType}", ActivityKind.Consumer, parent);
        activity?.SetTag("messaging.system", "voltflow.outbox");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.message.id", message.Id.ToString());
        activity?.SetTag("messaging.destination.name", message.EventType);
        return activity;
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
