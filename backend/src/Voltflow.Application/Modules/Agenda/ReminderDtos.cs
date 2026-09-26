namespace Voltflow.Application.Dtos;

public sealed record ReminderDto(
    Guid Id,
    string Type,
    string EntityName,
    Guid EntityId,
    DateTime DueAt,
    string Message,
    string State,
    int Attempts,
    DateTime NextAttemptAt,
    string? CompletionNote);

public sealed record CreateReminderRequest(
    string Type,
    string EntityName,
    Guid EntityId,
    DateTime DueAt,
    string Message);

public sealed record DismissReminderRequest(string? Note = null);
public sealed record CompleteReminderRequest(string? Note = null);
