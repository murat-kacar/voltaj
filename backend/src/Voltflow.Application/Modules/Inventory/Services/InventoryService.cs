using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Inventory;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;

    public InventoryService(IInventoryRepository repository, ICommandJournal commandJournal, IOperationContext operationContext)
    {
        _repository = repository;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
    }

    public async Task<Result<PagedResult<StockDto>>> ListAsync(string? search = null, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var page = await _repository.ListPagedAsync(
            search, PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        return Result<PagedResult<StockDto>>.Ok(page.Map(Map));
    }

    public async Task<Result<StockDto>> GetByMaterialCodeAsync(string materialCode, CancellationToken ct = default)
    {
        var stock = await _repository.GetByMaterialCodeAsync(materialCode, ct);
        return stock is null ? Result<StockDto>.Fail("Material not found.") : Result<StockDto>.Ok(Map(stock));
    }

    public async Task<Result<IReadOnlyList<StockDto>>> GetByMaterialCodesAsync(IReadOnlyCollection<string> materialCodes, CancellationToken ct = default)
    {
        var stocks = await _repository.GetByMaterialCodesAsync(materialCodes, ct);
        return Result<IReadOnlyList<StockDto>>.Ok(stocks.Select(Map).ToList());
    }

    public async Task<Result<StockDto>> AdjustAsync(AdjustStockRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialCode)) return Result<StockDto>.Fail("MaterialCode is required.");
        try
        {
            var stock = await _repository.AdjustWithMovementAsync(request.MaterialCode, request.Delta, "Manual stock adjustment", ct);
            // AdjustWithMovementAsync saves internally - own follow-up transaction, not piggybacked.
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: true, errorCode: null, ct);
            return Result<StockDto>.Ok(Map(stock));
        }
        catch (InvalidOperationException exception)
        {
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, "DOMAIN_VALIDATION_FAILED", ct);
            return Result<StockDto>.Fail(exception.Message);
        }
    }

    public async Task<Result> ApplyMovementAsync(string materialCode, decimal delta, Voltflow.Domain.Inventory.StockMovementType type, string reason, CancellationToken ct = default)
    {
        var stock = await _repository.GetByMaterialCodeAsync(materialCode, ct);
        if (stock is null) return Result.Fail("Material not found.");
        _repository.ApplyMovement(stock, delta, type, reason);
        return Result.Ok();
    }

    public async Task<Result<StockDto>> ReserveAsync(ReserveStockRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialCode)) return Result<StockDto>.Fail("MaterialCode is required.");
        var stock = await _repository.GetByMaterialCodeAsync(request.MaterialCode, ct);
        if (stock is null) return Result<StockDto>.Fail("Material not found.");

        try
        {
            stock.Reserve(request.Quantity);
            _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
            await _repository.UpdateAsync(stock, ct);
            return Result<StockDto>.Ok(Map(stock));
        }
        catch (InvalidOperationException exception)
        {
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, "DOMAIN_VALIDATION_FAILED", ct);
            return Result<StockDto>.Fail(exception.Message);
        }
    }

    public async Task<Result> ReceiveGoodsAsync(ReceiveGoodsRequest request, CancellationToken ct = default)
    {
        if (request.Lines.Count == 0) return Result.Fail("No lines provided.");

        var codes = request.Lines.Select(l => l.MaterialCode).ToHashSet();
        var stocks = await _repository.GetByMaterialCodesAsync(codes, ct);
        var stockDict = stocks.ToDictionary(s => s.MaterialCode);

        try
        {
            foreach (var line in request.Lines)
            {
                if (!stockDict.TryGetValue(line.MaterialCode, out var stock))
                    return Result.Fail($"Material not found: {line.MaterialCode}");
                
                _repository.ApplyMovement(stock, line.Quantity, StockMovementType.In, $"Goods Receipt: {request.InvoiceNumber} from {request.SupplierName}");
                await _repository.UpdateAsync(stock, ct);
            }

            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: true, errorCode: null, ct);
            return Result.Ok();
        }
        catch (InvalidOperationException exception)
        {
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, "DOMAIN_VALIDATION_FAILED", ct);
            return Result.Fail(exception.Message);
        }
    }

    private static StockDto Map(MaterialStock stock) => new(stock.MaterialCode, stock.Name, stock.QuantityOnHand, stock.ReservedQuantity, stock.AvailableQuantity);
}
