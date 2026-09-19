using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IInventoryService
{
    Task<Result<StockDto>> GetByMaterialCodeAsync(string materialCode, CancellationToken ct = default);
    Task<Result<StockDto>> AdjustAsync(AdjustStockRequest request, CancellationToken ct = default);
    Task<Result<StockDto>> ReserveAsync(ReserveStockRequest request, CancellationToken ct = default);
}
