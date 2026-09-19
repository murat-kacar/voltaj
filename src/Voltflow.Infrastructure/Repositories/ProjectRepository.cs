using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Projects;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(VoltflowDbContext dbContext) : base(dbContext) { }

    public override Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => DbContext.Projects.Include(x => x.Phases).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Project?> GetByNumberAsync(string number, CancellationToken ct = default)
        => DbContext.Projects.Include(x => x.Phases).FirstOrDefaultAsync(x => x.Number == number, ct);

    public async Task<IReadOnlyList<Project>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
        => await DbContext.Projects
            .Include(x => x.Phases)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public override async Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct = default)
        => await DbContext.Projects
            .Include(x => x.Phases)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<PagedResult<Project>> ListPagedAsync(int limit, int offset, CancellationToken ct = default)
    {
        var query = DbContext.Projects.Include(x => x.Phases).OrderByDescending(x => x.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<Project>(items, total, limit, offset);
    }
}

