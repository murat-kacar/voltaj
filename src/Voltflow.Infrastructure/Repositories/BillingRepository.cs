using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Projects;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class BillingRepository : Repository<BillingEntry>, IBillingRepository
{
    public BillingRepository(VoltflowDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<BillingEntry>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await DbContext.BillingEntries
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public override async Task<IReadOnlyList<BillingEntry>> ListAsync(CancellationToken ct = default)
        => await DbContext.BillingEntries
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<PagedResult<BillingEntry>> GetByProjectPagedAsync(Guid projectId, int limit, int offset, CancellationToken ct = default)
    {
        var query = DbContext.BillingEntries.Where(x => x.ProjectId == projectId).OrderByDescending(x => x.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<BillingEntry>(items, total, limit, offset);
    }
}

