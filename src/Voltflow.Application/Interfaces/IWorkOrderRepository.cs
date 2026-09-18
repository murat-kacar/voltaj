using Voltflow.Application.Common;
using Voltflow.Domain.WorkOrders;

namespace Voltflow.Application.Interfaces;

public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    Task<WorkOrder?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<WorkOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkOrder>> GetByAssignedUserAsync(Guid userId, CancellationToken ct = default);
    /// <summary>V8: assignedUserId null means no filter (dispatcher view); set means technician view.</summary>
    Task<PagedResult<WorkOrder>> ListPagedAsync(int limit, int offset, Guid? assignedUserId, CancellationToken ct = default);
}
