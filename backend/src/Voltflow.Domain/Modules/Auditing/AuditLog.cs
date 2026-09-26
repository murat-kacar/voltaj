using System;
using Voltflow.Domain.Common;

namespace Voltflow.Domain.Auditing;

public class AuditLog : Entity
{
    public Guid UserId { get; private set; }
    public string Action { get; private set; }
    public string EntityName { get; private set; }
    public string EntityId { get; private set; }
    public string Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    private AuditLog() { }

    public AuditLog(Guid userId, string action, string entityName, string entityId, string details, DateTime timestamp)
    {
        Id = Guid.NewGuid();
        UserId = Guard.AgainstEmptyGuid(userId, nameof(userId));
        Action = Guard.NotEmpty(action, nameof(action));
        EntityName = Guard.NotEmpty(entityName, nameof(entityName));
        EntityId = Guard.NotEmpty(entityId, nameof(entityId));
        Details = details ?? string.Empty;
        Timestamp = timestamp;
    }
}
