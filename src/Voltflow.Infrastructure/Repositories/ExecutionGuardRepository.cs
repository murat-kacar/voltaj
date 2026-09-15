using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class ExecutionGuardRepository : IExecutionGuard
{
    private readonly VoltflowDbContext _dbContext;

    public ExecutionGuardRepository(VoltflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExecutionGuardAcquireResult> TryAcquireAsync(string scope, string idempotencyKey, string requestHash, TimeSpan lifetime, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var expired = await _dbContext.ExecutionGuards.Where(x => x.State != ExecutionGuardState.Resolved && x.ExpiresAt <= now).ToListAsync(ct);
            if (expired.Count > 0)
            {
                _dbContext.ExecutionGuards.RemoveRange(expired);
                await _dbContext.SaveChangesAsync(ct);
            }
        }
        else
        {
            await _dbContext.ExecutionGuards
                .Where(x => x.State != ExecutionGuardState.Resolved && x.ExpiresAt <= now)
                .ExecuteDeleteAsync(ct);
        }

        var existing = await _dbContext.ExecutionGuards
            .SingleOrDefaultAsync(x => x.Scope == scope && x.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.State == ExecutionGuardState.Orphaned)
            {
                _dbContext.ExecutionGuards.Remove(existing);
                await _dbContext.SaveChangesAsync(ct);
            }
            else
            {
                if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                    return ExecutionGuardAcquireResult.RequestHashMismatch;
                return existing.State == ExecutionGuardState.Resolved
                    ? ExecutionGuardAcquireResult.AlreadyResolved
                    : ExecutionGuardAcquireResult.InProgress;
            }
        }

        _dbContext.ExecutionGuards.Add(new ExecutionGuardRecord(scope, idempotencyKey, requestHash, now.Add(lifetime)));
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return ExecutionGuardAcquireResult.Acquired;
        }
        catch (DbUpdateException)
        {
            return ExecutionGuardAcquireResult.InProgress;
        }
    }

    public async Task<(int StatusCode, string? ResponseBody)?> GetResolvedResponseAsync(string scope, string idempotencyKey, CancellationToken ct = default)
    {
        var guard = await _dbContext.ExecutionGuards
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Scope == scope && x.IdempotencyKey == idempotencyKey && x.State == ExecutionGuardState.Resolved, ct);
        return guard is null ? null : (guard.ResponseStatusCode, guard.ResponseBody);
    }

    public async Task ResolveAsync(string scope, string idempotencyKey, int statusCode, string? responseBody, CancellationToken ct = default)
    {
        var guard = await _dbContext.ExecutionGuards.SingleOrDefaultAsync(x => x.Scope == scope && x.IdempotencyKey == idempotencyKey, ct);
        if (guard is null) return;
        guard.Resolve(statusCode, responseBody);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task OrphanAsync(string scope, string idempotencyKey, CancellationToken ct = default)
    {
        var guard = await _dbContext.ExecutionGuards.SingleOrDefaultAsync(x => x.Scope == scope && x.IdempotencyKey == idempotencyKey, ct);
        if (guard is null) return;
        guard.Orphan();
        await _dbContext.SaveChangesAsync(ct);
    }
}