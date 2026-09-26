using Voltflow.Application.Common;
using Voltflow.Domain.Identity;

namespace Voltflow.Application.Interfaces;

public interface IAppUserRepository : IRepository<AppUser>
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>One page of the users by name.</summary>
    /// <param name="approved">Narrows the list to the approved (true) or the waiting (false) users; null lists everyone.</param>
    Task<PagedResult<AppUser>> ListPagedAsync(bool? approved, int limit, int offset, CancellationToken ct = default);
}
