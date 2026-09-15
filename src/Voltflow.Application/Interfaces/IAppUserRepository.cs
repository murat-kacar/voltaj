using Voltflow.Domain.Identity;

namespace Voltflow.Application.Interfaces;

public interface IAppUserRepository : IRepository<AppUser>
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
}
