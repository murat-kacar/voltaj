using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Inventory;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class InventoryRepository : Repository<MaterialStock>, IInventoryRepository
{
    public InventoryRepository(VoltflowDbContext dbContext) : base(dbContext) { }
    
    public override Task<MaterialStock?> GetByIdAsync(Guid id, CancellationToken ct = default) => DbContext.MaterialStocks.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<MaterialStock?> GetByMaterialCodeAsync(string materialCode, CancellationToken ct = default) => DbContext.MaterialStocks.FirstOrDefaultAsync(x => x.MaterialCode == materialCode, ct);
    public async Task<IReadOnlyList<MaterialStock>> GetLowStockAsync(CancellationToken ct = default) => await DbContext.MaterialStocks.Where(x => x.AvailableQuantity <= 0).OrderBy(x => x.MaterialCode).ToListAsync(ct);
    public override async Task<IReadOnlyList<MaterialStock>> ListAsync(CancellationToken ct = default) => await DbContext.MaterialStocks.OrderBy(x => x.MaterialCode).ToListAsync(ct);
    
    public async Task<PagedResult<MaterialStock>> ListPagedAsync(string? search, int limit, int offset, CancellationToken ct = default)
    {
        var query = DbContext.MaterialStocks.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.MaterialCode.ToLower().Contains(term) || x.Name.ToLower().Contains(term));
        }
        var ordered = query.OrderBy(x => x.MaterialCode).ThenBy(x => x.Id);
        var total = await ordered.CountAsync(ct);
        var items = await ordered.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<MaterialStock>(items, total, limit, offset);
    }

    public async Task<IReadOnlyList<MaterialStock>> GetByMaterialCodesAsync(IReadOnlyCollection<string> materialCodes, CancellationToken ct = default)
    {
        if (materialCodes.Count == 0) return [];
        var wanted = materialCodes.ToList();
        var rows = await DbContext.MaterialStocks
            .Where(x => wanted.Contains(x.MaterialCode))
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return rows.GroupBy(x => x.MaterialCode).Select(group => group.First()).ToList();
    }

    public void ApplyMovement(MaterialStock stock, decimal delta, StockMovementType type, string reason)
    {
        var previous = stock.QuantityOnHand;
        stock.Adjust(delta);
        DbContext.StockMovements.Add(new StockMovement(stock.MaterialCode, null, null, delta, type, previous, stock.QuantityOnHand, reason));
    }

    public async Task<MaterialStock> AdjustWithMovementAsync(string materialCode, decimal delta, string reason, CancellationToken ct = default) 
    { 
        var isRelational = DbContext.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = isRelational ? await DbContext.Database.BeginTransactionAsync(ct) : null;
        try
        {
            var stock = await GetByMaterialCodeAsync(materialCode, ct);
            if (stock is null)
            {
                var defaultWarehouseId = DbContext.Warehouses.Select(w => w.Id).FirstOrDefault();
                if (defaultWarehouseId == Guid.Empty)
                {
                    var wh = new Warehouse("Default Warehouse", WarehouseType.Main);
                    DbContext.Warehouses.Add(wh);
                    defaultWarehouseId = wh.Id;
                }
                stock = new MaterialStock(defaultWarehouseId, materialCode, "Auto-created material", 0);
                DbContext.MaterialStocks.Add(stock);
            }
            var previous = stock.QuantityOnHand;
            stock.Adjust(delta);
            DbContext.StockMovements.Add(new StockMovement(materialCode, null, null, delta, delta > 0 ? StockMovementType.In : StockMovementType.Out, previous, stock.QuantityOnHand, reason)); 
            await DbContext.SaveChangesAsync(ct); 
            if (transaction is not null) await transaction.CommitAsync(ct); 
            return stock; 
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }
}
