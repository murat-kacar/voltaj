using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Commands;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class CommandJournal : ICommandJournal
{
    private readonly VoltflowDbContext _dbContext;

    public CommandJournal(VoltflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task BeginAsync(Command command, CancellationToken ct = default)
    {
        var record = new CommandRecord(
            command.CommandId,
            command.ParentCommandId,
            command.ActorUserId,
            command.ActorRoles.Count > 0 ? string.Join(",", command.ActorRoles) : null,
            command.TriggerSource,
            command.CommandType,
            command.PayloadJson);
        _dbContext.CommandRecords.Add(record);
        await _dbContext.SaveChangesAsync(ct);
    }

    public void MarkResolved(Guid commandId, bool success, string? errorCode)
    {
        var tracked = _dbContext.ChangeTracker.Entries<CommandRecord>()
            .Select(entry => entry.Entity)
            .FirstOrDefault(entity => entity.CommandId == commandId);

        if (tracked is not null)
        {
            tracked.Resolve(success, errorCode);
            return;
        }

        // Not already tracked in this unit of work (e.g. a different DbContext instance in tests) -
        // load it so the caller's own SaveChanges still resolves it.
        var record = _dbContext.CommandRecords.FirstOrDefault(x => x.CommandId == commandId);
        record?.Resolve(success, errorCode);
    }

    public async Task ResolveNowAsync(Guid commandId, bool success, string? errorCode, CancellationToken ct = default)
    {
        MarkResolved(commandId, success, errorCode);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<bool> IsPendingAsync(Guid commandId, CancellationToken ct = default)
        => _dbContext.CommandRecords
            .AsNoTracking()
            .AnyAsync(x => x.CommandId == commandId && x.Status == CommandStatus.Pending, ct);
}
