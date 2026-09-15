using Voltflow.Domain.Inventory;

namespace Voltflow.Application.Interfaces;

public interface IInventoryRepository : IRepository<MaterialStock>
{
    Task<MaterialStock?> GetByMaterialCodeAsync(string materialCode, CancellationToken ct = default);
    Task<IReadOnlyList<MaterialStock>> GetLowStockAsync(CancellationToken ct = default);
    Task<MaterialStock> AdjustWithMovementAsync(string materialCode, decimal delta, string reason, CancellationToken ct = default);
}
