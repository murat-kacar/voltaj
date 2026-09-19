using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Inventory;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(VoltflowDbContext dbContext) : base(dbContext) { }

    public Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var key = code.Trim().ToLowerInvariant();
        return DbContext.Products.FirstOrDefaultAsync(x => x.Code.ToLower() == key, ct);
    }

    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var key = barcode.Trim();
        return DbContext.Products.FirstOrDefaultAsync(x => x.Barcode == key, ct);
    }

    public async Task<Product?> FindAsync(string term, CancellationToken ct = default)
    {
        var key = term.Trim();
        if (key.Length == 0) return null;
        return await GetByBarcodeAsync(key, ct) ?? await GetByCodeAsync(key, ct);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        var wanted = ids.ToList();
        return await DbContext.Products.Where(x => wanted.Contains(x.Id)).ToListAsync(ct);
    }

    public async Task<PagedResult<Product>> ListPagedAsync(string? search, bool activeOnly, int limit, int offset, CancellationToken ct = default)
    {
        var query = DbContext.Products.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(term)
                                     || x.Code.ToLower().Contains(term)
                                     || (x.Barcode != null && x.Barcode.ToLower().Contains(term)));
        }

        var ordered = query.OrderBy(x => x.Name).ThenBy(x => x.Code);
        var total = await ordered.CountAsync(ct);
        var items = await ordered.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<Product>(items, total, limit, offset);
    }
}
