using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.WorkOrders;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class WorkOrderRepository : Repository<WorkOrder>, IWorkOrderRepository
{
    public WorkOrderRepository(VoltflowDbContext dbContext) : base(dbContext) { }

    public override Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken ct = default) => DbContext.WorkOrders.Include(x => x.Items).Include(x => x.TimeEntries).FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<WorkOrder?> GetByNumberAsync(string number, CancellationToken ct = default) => DbContext.WorkOrders.Include(x => x.Items).Include(x => x.TimeEntries).FirstOrDefaultAsync(x => x.Number == number, ct);
    public async Task<IReadOnlyList<WorkOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) => await DbContext.WorkOrders.Include(x => x.Items).Include(x => x.TimeEntries).Where(x => x.CustomerId == customerId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    public async Task<IReadOnlyList<WorkOrder>> GetByAssignedUserAsync(Guid userId, CancellationToken ct = default) => await DbContext.WorkOrders.Include(x => x.Items).Include(x => x.TimeEntries).Where(x => x.AssignedUserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    public override async Task<IReadOnlyList<WorkOrder>> ListAsync(CancellationToken ct = default) => await DbContext.WorkOrders.Include(x => x.Items).Include(x => x.TimeEntries).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
}
