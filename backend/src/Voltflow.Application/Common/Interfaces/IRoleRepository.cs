using Voltflow.Domain.Identity;

namespace Voltflow.Application.Interfaces;

public interface IRoleRepository : IRepository<AppRole>
{
    Task<AppRole?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetNamesByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>The role names of several users at once; a user without a role has no entry.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetNamesByUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
}
