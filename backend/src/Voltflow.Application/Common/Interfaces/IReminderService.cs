using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IReminderService
{
    Task<Result<PagedResult<ReminderDto>>> ListAsync(string? state = null, int? limit = null, int? offset = null, CancellationToken ct = default);
    Task<Result<ReminderDto>> CreateAsync(CreateReminderRequest request, CancellationToken ct = default);
    Task<Result<ReminderDto>> DismissAsync(Guid id, DismissReminderRequest request, CancellationToken ct = default);
    Task<Result<ReminderDto>> CompleteAsync(Guid id, CompleteReminderRequest request, CancellationToken ct = default);
}
