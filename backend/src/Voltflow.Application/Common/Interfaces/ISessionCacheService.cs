namespace Voltflow.Application.Interfaces;

public interface ISessionCacheService
{
    Task<bool> IsSessionValidAsync(string token, CancellationToken ct = default);
    Task InvalidateSessionAsync(string token, TimeSpan? blacklistTtl = null, CancellationToken ct = default);
}
