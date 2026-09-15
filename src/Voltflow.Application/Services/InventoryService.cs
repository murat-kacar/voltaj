using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Inventory;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;
    public InventoryService(IInventoryRepository repository) => _repository = repository;

    public async Task<Result<StockDto>> GetByMaterialCodeAsync(string materialCode, CancellationToken ct = default)
    {
        var stock = await _repository.GetByMaterialCodeAsync(materialCode, ct);
        return stock is null ? Result<StockDto>.Fail("Material not found.") : Result<StockDto>.Ok(Map(stock));
    }

    public async Task<Result<StockDto>> AdjustAsync(AdjustStockRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialCode)) return Result<StockDto>.Fail("MaterialCode is required.");
        try { return Result<StockDto>.Ok(Map(await _repository.AdjustWithMovementAsync(request.MaterialCode, request.Delta, "Manual stock adjustment", ct))); }
        catch (InvalidOperationException exception) { return Result<StockDto>.Fail(exception.Message); }
    }

    public async Task<Result<StockDto>> ReserveAsync(ReserveStockRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialCode)) return Result<StockDto>.Fail("MaterialCode is required.");
        var stock = await _repository.GetByMaterialCodeAsync(request.MaterialCode, ct);
        if (stock is null) return Result<StockDto>.Fail("Material not found.");

        try
        {
            stock.Reserve(request.Quantity);
            await _repository.UpdateAsync(stock, ct);
            return Result<StockDto>.Ok(Map(stock));
        }
        catch (InvalidOperationException exception)
        {
            return Result<StockDto>.Fail(exception.Message);
        }
    }

    private static StockDto Map(MaterialStock stock) => new(stock.MaterialCode, stock.Name, stock.QuantityOnHand, stock.ReservedQuantity, stock.AvailableQuantity);
}
