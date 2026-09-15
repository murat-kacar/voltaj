using Voltflow.Domain.Identity;

namespace Voltflow.Application.Interfaces;

public interface IUserSessionRepository
{
    Task AddAsync(UserSession session, CancellationToken ct = default);
    Task<UserSession?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task UpdateAsync(UserSession session, CancellationToken ct = default);
}
