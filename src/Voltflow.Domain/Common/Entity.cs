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

    public void SetUpdatedContext(Guid? userId, string endpoint, Guid operationId)
    {
        UpdatedByUserId = userId;
        UpdatedByEndpoint = endpoint;
        UpdatedInOperationId = operationId;
        Touch();
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void IncrementVersion()
    {
        if (Version == long.MaxValue) throw new InvalidOperationException("Entity version limit reached.");
        Version++;
    }
}
