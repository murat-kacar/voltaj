using Voltflow.Application.Interfaces;

namespace Voltflow.Infrastructure.Security;

public sealed class DirectDbSessionCacheService : ISessionCacheService
{
    private readonly IUserSessionRepository _sessions;

    public DirectDbSessionCacheService(IUserSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<bool> IsSessionValidAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var session = await _sessions.GetByTokenAsync(token, ct);
        return session is not null && session.IsActive;
    }

    public Task InvalidateSessionAsync(string token, TimeSpan? blacklistTtl = null, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
