using Voltflow.Domain.Reminders;

namespace Voltflow.Application.Interfaces;

public interface IReminderRepository
{
    Task AddAsync(ReminderRecord reminder, CancellationToken ct = default);
    Task<IReadOnlyList<ReminderRecord>> ListDueAsync(DateTime utcNow, CancellationToken ct = default);
    Task UpdateAsync(ReminderRecord reminder, CancellationToken ct = default);
}