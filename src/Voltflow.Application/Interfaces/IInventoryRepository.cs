using Voltflow.Domain.Inventory;

namespace Voltflow.Application.Interfaces;

public interface IInventoryRepository : IRepository<MaterialStock>
{
    Task<MaterialStock?> GetByMaterialCodeAsync(string materialCode, CancellationToken ct = default);
    Task<IReadOnlyList<MaterialStock>> GetLowStockAsync(CancellationToken ct = default);
    Task<MaterialStock> AdjustWithMovementAsync(string materialCode, decimal delta, string reason, CancellationToken ct = default);

    /// <summary>The stock row of each code that has one (one row per code). Tracked, so the caller can go on to move stock.</summary>
    Task<IReadOnlyList<MaterialStock>> GetByMaterialCodesAsync(IReadOnlyCollection<string> materialCodes, CancellationToken ct = default);

    /// <summary>Moves stock and records the movement without saving; the caller commits together with its own changes.
    /// Throws if the stock would go below zero, so check availability first.</summary>
    void ApplyMovement(MaterialStock stock, decimal delta, StockMovementType type, string reason);
}
