using Voltflow.Application.Common;
using Voltflow.Domain.Projects;

namespace Voltflow.Application.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<PagedResult<Project>> ListPagedAsync(int limit, int offset, CancellationToken ct = default);
}
