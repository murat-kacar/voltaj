using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Globalization;
using Voltflow.Application.Interfaces;

namespace Voltflow.Infrastructure.RateLimiting;

public sealed class RedisRateLimiter : IDistributedRateLimiter
{
    private const string SlidingWindowScript = "local now = tonumber(ARGV[1]); local window = tonumber(ARGV[2]); local limit = tonumber(ARGV[3]); redis.call('ZREMRANGEBYSCORE', KEYS[1], 0, now - window); local count = redis.call('ZCARD', KEYS[1]); if count >= limit then local first = redis.call('ZRANGE', KEYS[1], 0, 0, 'WITHSCORES'); local retry = window - (now - tonumber(first[2])); return {0, retry}; end; redis.call('ZADD', KEYS[1], now, ARGV[4]); redis.call('EXPIRE', KEYS[1], math.ceil(window / 1000) + 1); return {1, 0}";
    private readonly Lazy<ConnectionMultiplexer> _connection;
    private readonly string _prefix;

    public RedisRateLimiter(IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        _prefix = configuration["Redis:RateLimitPrefix"] ?? "voltflow";
        _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connectionString));
    }

    public async Task<RateLimitDecision> CheckAsync(string partition, int permitLimit, TimeSpan window, CancellationToken ct = default)
    {
        var database = _connection.Value.GetDatabase();
        var key = $"{_prefix}:rate:{partition}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = (RedisResult[]?)await database.ScriptEvaluateAsync(
            SlidingWindowScript,
            [new RedisKey(key)],
            new RedisValue[]
            {
                new(now.ToString(CultureInfo.InvariantCulture)),
                new(((long)window.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)),
                new(permitLimit.ToString(CultureInfo.InvariantCulture)),
                new(Guid.NewGuid().ToString("N"))
            });
        if (result is null || result.Length < 2)
            throw new InvalidOperationException("Redis rate-limit script returned an invalid result.");
        var allowed = (int)result[0] == 1;
        var retryMilliseconds = allowed ? 0 : (long)result[1];
        return new RateLimitDecision(allowed, Math.Max(1, (int)Math.Ceiling(retryMilliseconds / 1000d)));
    }
}