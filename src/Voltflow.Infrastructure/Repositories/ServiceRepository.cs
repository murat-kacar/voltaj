using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Services;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class ServiceRepository : Repository<Service>, IServiceRepository
{
    private readonly VoltflowDbContext _context;

    public ServiceRepository(VoltflowDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Services
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<PagedResult<Service>> ListPagedAsync(ServiceFilter filter, int limit, int offset, CancellationToken ct = default)
    {
        var query = _context.Services.AsQueryable();

        if (filter.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == filter.CustomerId.Value);

        if (filter.AssignedUserId.HasValue)
            query = query.Where(s => s.AssignedUserId == filter.AssignedUserId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<ServiceStatus>(filter.Status, ignoreCase: true, out var status))
            query = query.Where(s => s.Status == status);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(x => x.Number.ToLower().Contains(s) || x.Title.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return new PagedResult<Service>(items, total, limit, offset);
    }
}
