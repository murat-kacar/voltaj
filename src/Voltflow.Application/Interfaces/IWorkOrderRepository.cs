using Voltflow.Domain.WorkOrders;

namespace Voltflow.Application.Interfaces;

public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    Task<WorkOrder?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<WorkOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkOrder>> GetByAssignedUserAsync(Guid userId, CancellationToken ct = default);
}
