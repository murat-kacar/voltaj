using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Reminders;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class ReminderRepository : IReminderRepository
{
    private readonly VoltflowDbContext _dbContext;

    public ReminderRepository(VoltflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ReminderRecord reminder, CancellationToken ct = default)
    {
        await _dbContext.ReminderRecords.AddAsync(reminder, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ReminderRecord>> ListDueAsync(DateTime utcNow, CancellationToken ct = default)
        => await _dbContext.ReminderRecords
            .Where(x => x.State == ReminderState.Pending && x.NextAttemptAt <= utcNow)
            .OrderBy(x => x.NextAttemptAt)
            .Take(100)
            .ToListAsync(ct);

    public async Task UpdateAsync(ReminderRecord reminder, CancellationToken ct = default)
    {
        _dbContext.ReminderRecords.Update(reminder);
        await _dbContext.SaveChangesAsync(ct);
    }
}