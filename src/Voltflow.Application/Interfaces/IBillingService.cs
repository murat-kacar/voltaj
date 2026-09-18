using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IBillingService
{
    Task<Result<BillingEntryDto>> CreateAsync(Guid projectId, Guid customerId, decimal amount, CancellationToken ct = default);
    Task<Result<BillingEntryDto>> CreateAsync(Guid projectId, CreateBillingEntryRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<BillingEntryDto>>> ListAsync(Guid projectId, int? limit = null, int? offset = null, CancellationToken ct = default);
}
