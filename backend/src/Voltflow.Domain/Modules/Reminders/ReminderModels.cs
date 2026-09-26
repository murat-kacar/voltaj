using Voltflow.Domain.Common;

namespace Voltflow.Domain.Reminders;

public enum ReminderState
{
    Pending,
    Completed,
    Dismissed
}

public sealed class ReminderRecord : Entity
{
    public string Type { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTime DueAt { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public ReminderState State { get; private set; }
    public int Attempts { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public string? CompletionNote { get; private set; }

    private ReminderRecord() { }

    public ReminderRecord(string type, string entityName, Guid entityId, DateTime dueAt, string message)
    {
        Type = Guard.NotEmpty(type, nameof(type));
        EntityName = Guard.NotEmpty(entityName, nameof(entityName));
        EntityId = Guard.AgainstEmptyGuid(entityId, nameof(entityId));
        DueAt = dueAt;
        Message = Guard.NotEmpty(message, nameof(message));
        State = ReminderState.Pending;
        NextAttemptAt = dueAt;
    }

    public void RegisterAttempt()
    {
        if (State != ReminderState.Pending) return;
        Attempts++;
        NextAttemptAt = DateTime.UtcNow.AddMinutes(Math.Min(Attempts * 5, 60));
        Touch();
    }

    public void Complete(string? note = null)
    {
        if (State != ReminderState.Pending) throw new InvalidOperationException("Only pending reminders can complete.");
        State = ReminderState.Completed;
        CompletionNote = note;
        Touch();
    }

    public void Dismiss(string? note = null)
    {
        if (State != ReminderState.Pending) throw new InvalidOperationException("Only pending reminders can dismiss.");
        State = ReminderState.Dismissed;
        CompletionNote = note;
        Touch();
    }
}