using System.Collections.Concurrent;
using Voltflow.Application.Interfaces;

namespace Voltflow.Infrastructure.RateLimiting;

public sealed class InMemoryRateLimiter : IDistributedRateLimiter
{
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _buckets = new();

    public Task<RateLimitDecision> CheckAsync(string partition, int permitLimit, TimeSpan window, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var timestamps = _buckets.GetOrAdd(partition, _ => new List<DateTimeOffset>());
        lock (timestamps)
        {
            timestamps.RemoveAll(ts => ts < now - window);
            if (timestamps.Count >= permitLimit)
            {
                var oldest = timestamps[0];
                var retrySec = Math.Max(1, (int)Math.Ceiling((oldest + window - now).TotalSeconds));
                return Task.FromResult(new RateLimitDecision(false, retrySec));
            }
            timestamps.Add(now);
            return Task.FromResult(new RateLimitDecision(true, 0));
        }
    }
}
