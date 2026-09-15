using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Common;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public abstract class Repository<T> : IRepository<T> where T : Entity
{
    protected readonly VoltflowDbContext DbContext;

    protected Repository(VoltflowDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return DbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
    {
        return await DbContext.Set<T>().ToListAsync(ct);
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await DbContext.Set<T>().AddAsync(entity, ct);
        await DbContext.SaveChangesAsync(ct);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        DbContext.Set<T>().Update(entity);
        await DbContext.SaveChangesAsync(ct);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is not null)
        {
            DbContext.Set<T>().Remove(entity);
            await DbContext.SaveChangesAsync(ct);
        }
    }
}
