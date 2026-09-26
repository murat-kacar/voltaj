using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Reminders;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class ReminderService : IReminderService
{
    private readonly IReminderRepository _repository;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;

    public ReminderService(IReminderRepository repository, ICommandJournal commandJournal, IOperationContext operationContext)
    {
        _repository = repository;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
    }

    public async Task<Result<PagedResult<ReminderDto>>> ListAsync(string? state = null, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        ReminderState? parsedState = null;
        if (!string.IsNullOrEmpty(state) && Enum.TryParse<ReminderState>(state, true, out var s))
            parsedState = s;

        var page = await _repository.ListAsync(
            parsedState,
            PaginationDefaults.NormalizeLimit(limit),
            PaginationDefaults.NormalizeOffset(offset),
            ct);
        return Result<PagedResult<ReminderDto>>.Ok(page.Map(ToDto));
    }

    public async Task<Result<ReminderDto>> CreateAsync(CreateReminderRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Type)) return Result<ReminderDto>.Fail("Type is required.");
        if (string.IsNullOrWhiteSpace(request.EntityName)) return Result<ReminderDto>.Fail("EntityName is required.");
        if (request.EntityId == Guid.Empty) return Result<ReminderDto>.Fail("EntityId is required.");
        if (string.IsNullOrWhiteSpace(request.Message)) return Result<ReminderDto>.Fail("Message is required.");

        var reminder = new ReminderRecord(request.Type, request.EntityName, request.EntityId, request.DueAt.ToUniversalTime(), request.Message);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _repository.AddAsync(reminder, ct);
        return Result<ReminderDto>.Ok(ToDto(reminder));
    }

    public async Task<Result<ReminderDto>> DismissAsync(Guid id, DismissReminderRequest request, CancellationToken ct = default)
    {
        var reminder = await _repository.GetByIdAsync(id, ct);
        if (reminder is null) return Result<ReminderDto>.Fail("Reminder not found.");
        try { reminder.Dismiss(request.Note); }
        catch (InvalidOperationException ex) { return Result<ReminderDto>.Fail(ex.Message); }
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _repository.UpdateAsync(reminder, ct);
        return Result<ReminderDto>.Ok(ToDto(reminder));
    }

    public async Task<Result<ReminderDto>> CompleteAsync(Guid id, CompleteReminderRequest request, CancellationToken ct = default)
    {
        var reminder = await _repository.GetByIdAsync(id, ct);
        if (reminder is null) return Result<ReminderDto>.Fail("Reminder not found.");
        try { reminder.Complete(request.Note); }
        catch (InvalidOperationException ex) { return Result<ReminderDto>.Fail(ex.Message); }
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _repository.UpdateAsync(reminder, ct);
        return Result<ReminderDto>.Ok(ToDto(reminder));
    }

    private static ReminderDto ToDto(ReminderRecord r) =>
        new(r.Id, r.Type, r.EntityName, r.EntityId, r.DueAt, r.Message,
            r.State.ToString(), r.Attempts, r.NextAttemptAt, r.CompletionNote);
}
