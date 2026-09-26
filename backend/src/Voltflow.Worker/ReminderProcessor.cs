using Voltflow.Application.Interfaces;

namespace Voltflow.Worker;

public sealed class ReminderProcessor
{
    private readonly IReminderRepository _repository;

    public ReminderProcessor(IReminderRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> ProcessDueAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var reminders = await _repository.ListDueAsync(utcNow, ct);
        foreach (var reminder in reminders)
        {
            reminder.RegisterAttempt();
            await _repository.UpdateAsync(reminder, ct);
        }

        return reminders.Count;
    }
}
