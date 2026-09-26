using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IQuickSaleService
{
    Task<Result<QuickSaleDto>> CompleteSaleAsync(StartQuickSaleRequest request, CancellationToken ct = default);
    Task<Result<QuickSaleDto>> VoidSaleAsync(Guid id, string reason, CancellationToken ct = default);
    Task<Result<QuickSaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<QuickSaleSummaryDto>>> ListAsync(int? limit = null, int? offset = null, CancellationToken ct = default);
}
