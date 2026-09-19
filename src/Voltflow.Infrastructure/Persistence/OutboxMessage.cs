using System.Diagnostics;
using Voltflow.Domain.Common;

namespace Voltflow.Infrastructure.Persistence;

public sealed class OutboxMessage : Entity
{
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public OutboxState State { get; private set; }
    public string? TraceParent { get; private set; }
    public string? TraceState { get; private set; }
    public const int MaxAttempts = 5;

    private OutboxMessage() { }

    public OutboxMessage(string eventType, string payloadJson)
    {
        EventType = Guard.NotEmpty(eventType, nameof(eventType));
        PayloadJson = Guard.NotEmpty(payloadJson, nameof(payloadJson));
        OccurredAt = DateTime.UtcNow;
        NextAttemptAt = OccurredAt;
        State = OutboxState.Pending;

        // T2: the outbox is the asynchronous service boundary (API process -> Worker process). Capturing
        // the producing span's W3C trace context here - the one constructor every enqueue path goes
        // through - lets the Worker continue the same trace instead of starting an unrelated one.
        var current = Activity.Current;
        if (current is { IdFormat: ActivityIdFormat.W3C })
        {
            TraceParent = current.Id;
            TraceState = current.TraceStateString;
        }
    }

    public void MarkAttempt(string? error = null)
    {
        Attempts++;
        LastError = error;
        if (error is null)
        {
            ProcessedAt = DateTime.UtcNow;
            State = OutboxState.Processed;
        }
        else if (Attempts >= MaxAttempts)
        {
            State = OutboxState.DeadLetter;
        }
        else
        {
            State = OutboxState.Pending;
            NextAttemptAt = DateTime.UtcNow.AddMinutes(Math.Min(Math.Pow(2, Attempts), 60));
        }
        Touch();
    }

    public void Requeue()
    {
        State = OutboxState.Pending;
        NextAttemptAt = DateTime.UtcNow;
        LastError = null;
        Touch();
    }
}

public enum OutboxState
{
    Pending,
    Processed,
    DeadLetter
}
