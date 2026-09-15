using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Voltflow.Application.Interfaces;

namespace Voltflow.Infrastructure.Security;

public sealed class RedisSessionCacheService : ISessionCacheService
{
    private static readonly TimeSpan DefaultActiveCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DefaultBlacklistTtl = TimeSpan.FromHours(1);

    private readonly Lazy<ConnectionMultiplexer> _connection;
    private readonly IUserSessionRepository _sessions;
    private readonly string _prefix;

    public RedisSessionCacheService(IConfiguration configuration, IUserSessionRepository sessions)
    {
        var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        _prefix = configuration["Redis:RateLimitPrefix"] ?? "voltflow";
        _sessions = sessions;
        _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connectionString));
    }

    public async Task<bool> IsSessionValidAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var tokenHash = ComputeHash(token);
        var activeKey = $"{_prefix}:session:active:{tokenHash}";
        var revokedKey = $"{_prefix}:session:revoked:{tokenHash}";

        try
        {
            var database = _connection.Value.GetDatabase();

            // 1. Check blacklist first (instant rejection if revoked)
            if (await database.KeyExistsAsync(revokedKey))
                return false;

            // 2. Check active cache (fast path, no DB query)
            if (await database.KeyExistsAsync(activeKey))
                return true;
        }
        catch
        {
            // Redis error fallback: proceed to DB check below
        }

        // 3. Cache miss: verify against database
        var session = await _sessions.GetByTokenAsync(token, ct);
        if (session is null || !session.IsActive)
        {
            try
            {
                var database = _connection.Value.GetDatabase();
                await database.StringSetAsync(revokedKey, "1", DefaultBlacklistTtl);
            }
            catch
            {
                // Ignored if Redis is unavailable
            }
            return false;
        }

        // 4. Session is active: populate active cache with short TTL
        try
        {
            var remainingSessionTime = session.ExpiresAt - DateTimeOffset.UtcNow;
            var ttl = remainingSessionTime > TimeSpan.Zero
                ? (remainingSessionTime < DefaultActiveCacheTtl ? remainingSessionTime : DefaultActiveCacheTtl)
                : TimeSpan.FromMinutes(1);

            var database = _connection.Value.GetDatabase();
            await database.StringSetAsync(activeKey, "1", ttl);
        }
        catch
        {
            // Ignored if Redis is unavailable
        }

        return true;
    }

    public async Task InvalidateSessionAsync(string token, TimeSpan? blacklistTtl = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        var tokenHash = ComputeHash(token);
        var activeKey = $"{_prefix}:session:active:{tokenHash}";
        var revokedKey = $"{_prefix}:session:revoked:{tokenHash}";
        var duration = blacklistTtl ?? DefaultBlacklistTtl;

        try
        {
            var database = _connection.Value.GetDatabase();
            await database.KeyDeleteAsync(activeKey);
            await database.StringSetAsync(revokedKey, "1", duration);
        }
        catch
        {
            // Redis failure should not block session revocation in DB
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
