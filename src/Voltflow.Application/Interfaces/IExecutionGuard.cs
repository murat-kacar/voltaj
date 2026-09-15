namespace Voltflow.Application.Interfaces;

public interface IExecutionGuard
{
    Task<ExecutionGuardAcquireResult> TryAcquireAsync(string scope, string idempotencyKey, string requestHash, TimeSpan lifetime, CancellationToken ct = default);
    Task<(int StatusCode, string? ResponseBody)?> GetResolvedResponseAsync(string scope, string idempotencyKey, CancellationToken ct = default);
    Task ResolveAsync(string scope, string idempotencyKey, int statusCode, string? responseBody, CancellationToken ct = default);
    Task OrphanAsync(string scope, string idempotencyKey, CancellationToken ct = default);
}

public enum ExecutionGuardAcquireResult
{
    Acquired,
    AlreadyResolved,
    InProgress,
    RequestHashMismatch
}

public interface IDistributedRateLimiter
{
    Task<RateLimitDecision> CheckAsync(string partition, int permitLimit, TimeSpan window, CancellationToken ct = default);
}

public sealed record RateLimitDecision(bool Allowed, int RetryAfterSeconds);