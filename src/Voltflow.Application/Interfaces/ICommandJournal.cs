using Voltflow.Application.Commands;

namespace Voltflow.Application.Interfaces;

/// <summary>
/// H9 (intent-first / WAL): <see cref="BeginAsync"/> writes a `pending` record in its own, immediate
/// transaction, before the mutation runs. <see cref="MarkResolved"/> only updates the tracked entity
/// in the current unit of work - it does not save; callers that want the resolution to land in the
/// same transaction as their own business write call it before their own SaveChanges. Callers that
/// have no business write of their own (e.g. the pipeline's own fallback) use
/// <see cref="ResolveNowAsync"/> instead, which saves immediately.
/// </summary>
public interface ICommandJournal
{
    Task BeginAsync(Command command, CancellationToken ct = default);
    void MarkResolved(Guid commandId, bool success, string? errorCode);
    Task ResolveNowAsync(Guid commandId, bool success, string? errorCode, CancellationToken ct = default);
    Task<bool> IsPendingAsync(Guid commandId, CancellationToken ct = default);
}
