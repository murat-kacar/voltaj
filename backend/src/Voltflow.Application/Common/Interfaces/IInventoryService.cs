using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IInventoryService
{
    /// <param name="search">Part of the material code or name.</param>
    Task<Result<PagedResult<StockDto>>> ListAsync(string? search = null, int? limit = null, int? offset = null, CancellationToken ct = default);

    Task<Result<StockDto>> GetByMaterialCodeAsync(string materialCode, CancellationToken ct = default);
    Task<Result<IReadOnlyList<StockDto>>> GetByMaterialCodesAsync(IReadOnlyCollection<string> materialCodes, CancellationToken ct = default);
    Task<Result<StockDto>> AdjustAsync(AdjustStockRequest request, CancellationToken ct = default);
    Task<Result> ApplyMovementAsync(string materialCode, decimal delta, Voltflow.Domain.Inventory.StockMovementType type, string reason, CancellationToken ct = default);
    Task<Result<StockDto>> ReserveAsync(ReserveStockRequest request, CancellationToken ct = default);
    Task<Result> ReceiveGoodsAsync(ReceiveGoodsRequest request, CancellationToken ct = default);
}
