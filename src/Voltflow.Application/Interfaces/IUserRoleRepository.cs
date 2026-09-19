using Voltflow.Domain.Identity;

namespace Voltflow.Application.Interfaces;

public interface IUserRoleRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken ct = default);
    Task AddAsync(AppUserRole userRole, CancellationToken ct = default);
}
