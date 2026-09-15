using Voltflow.Domain.WorkOrders;

namespace Voltflow.Application.Interfaces;

public interface IMaintenanceContractRepository
{
    Task<IReadOnlyList<MaintenanceContract>> GetDueContractsAsync(DateTime currentDate, CancellationToken ct = default);
    Task UpdateAsync(MaintenanceContract contract, CancellationToken ct = default);
}
