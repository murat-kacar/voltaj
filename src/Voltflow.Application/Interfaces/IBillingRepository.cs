using Voltflow.Domain.Projects;

namespace Voltflow.Application.Interfaces;

public interface IBillingRepository : IRepository<BillingEntry>
{
    Task<IReadOnlyList<BillingEntry>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
}
