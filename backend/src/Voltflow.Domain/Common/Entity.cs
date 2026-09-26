namespace Voltflow.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public long Version { get; protected set; } = 1;
    public Guid? CreatedByUserId { get; protected set; }
    public Guid? UpdatedByUserId { get; protected set; }
    public string? CreatedByEndpoint { get; protected set; }
    public string? UpdatedByEndpoint { get; protected set; }
    public Guid? CreatedInOperationId { get; protected set; }
    public Guid? UpdatedInOperationId { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    public void SetCreatedContext(Guid? userId, string endpoint, Guid operationId)
    {
        if (CreatedByUserId is null && userId is not null) CreatedByUserId = userId;
        CreatedByEndpoint ??= endpoint;
        CreatedInOperationId ??= operationId;
    }

    /// <summary>V7: overrides the constructor's own UtcNow with the pipeline's injected clock, so
    /// CreatedAt reflects TimeProvider rather than an untestable direct system-clock read.</summary>
    public void SetCreatedAt(DateTime utcNow) => CreatedAt = utcNow;

    public void SetUpdatedContext(Guid? userId, string endpoint, Guid operationId, DateTime utcNow)
    {
        UpdatedByUserId = userId;
        UpdatedByEndpoint = endpoint;
        UpdatedInOperationId = operationId;
        Touch(utcNow);
    }

    /// <summary>V7: system clock is injected - callers on the audited pipeline (VoltflowDbContext)
    /// pass TimeProvider's value; this parameterless overload remains only for domain code
    /// constructing entities outside that pipeline (e.g. plain unit tests).</summary>
    public void Touch() => UpdatedAt = DateTime.UtcNow;
    public void Touch(DateTime utcNow) => UpdatedAt = utcNow;

    public void IncrementVersion()
    {
        if (Version == long.MaxValue) throw new InvalidOperationException("Entity version limit reached.");
        Version++;
    }
}
