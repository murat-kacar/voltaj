using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Sales;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class QuickSaleRepository : IQuickSaleRepository
{
    private readonly VoltflowDbContext _dbContext;

    public QuickSaleRepository(VoltflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QuickSale?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Set<QuickSale>()
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<QuickSale>> ListAsync(CancellationToken ct = default)
    {
        return await _dbContext.Set<QuickSale>().ToListAsync(ct);
    }

    public async Task AddAsync(QuickSale entity, CancellationToken ct = default)
    {
        await _dbContext.Set<QuickSale>().AddAsync(entity, ct);
    }

    public Task UpdateAsync(QuickSale entity, CancellationToken ct = default)
    {
        _dbContext.Set<QuickSale>().Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is not null)
        {
            _dbContext.Set<QuickSale>().Remove(entity);
        }
    }

    public async Task<PagedResult<QuickSale>> ListPagedAsync(int limit, int offset, CancellationToken ct = default)
    {
        var query = _dbContext.Set<QuickSale>().AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.SoldAt).Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<QuickSale>(items, total, limit, offset);
    }
}
