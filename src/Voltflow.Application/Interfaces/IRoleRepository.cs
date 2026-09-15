using Voltflow.Domain.Identity;

namespace Voltflow.Application.Interfaces;

public interface IRoleRepository : IRepository<AppRole>
{
    Task<AppRole?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetNamesByUserAsync(Guid userId, CancellationToken ct = default);
}
