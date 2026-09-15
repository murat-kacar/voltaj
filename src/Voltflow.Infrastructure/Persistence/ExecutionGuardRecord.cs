using Voltflow.Domain.Common;

namespace Voltflow.Infrastructure.Persistence;

public sealed class ExecutionGuardRecord : Entity
{
    public ExecutionGuardState State { get; private set; }
    public string Scope { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public int ResponseStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }

    private ExecutionGuardRecord() { }

    public ExecutionGuardRecord(string scope, string idempotencyKey, string requestHash, DateTime expiresAt)
    {
        Scope = Guard.NotEmpty(scope, nameof(scope));
        IdempotencyKey = Guard.NotEmpty(idempotencyKey, nameof(idempotencyKey));
        RequestHash = Guard.NotEmpty(requestHash, nameof(requestHash));
        ExpiresAt = expiresAt;
        State = ExecutionGuardState.Pending;
    }

    public void Resolve(int statusCode, string? responseBody)
    {
        State = ExecutionGuardState.Resolved;
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
    }
    public void Orphan() => State = ExecutionGuardState.Orphaned;
}

public enum ExecutionGuardState
{
    Pending,
    Resolved,
    Orphaned
}