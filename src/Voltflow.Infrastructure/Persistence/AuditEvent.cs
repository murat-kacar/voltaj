using Voltflow.Domain.Common;

namespace Voltflow.Infrastructure.Persistence;

public sealed class AuditEvent : Entity
{
    public Guid OperationId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? ParentOperationId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string SourceEndpoint { get; private set; } = string.Empty;
    public string? SourceScreen { get; private set; }
    public string? SourceAction { get; private set; }
    public string BeforeJson { get; private set; } = "{}";
    public string AfterJson { get; private set; } = "{}";
    public string ChangedFieldsJson { get; private set; } = "[]";

    private AuditEvent() { }

    public AuditEvent(
        Guid operationId,
        Guid? parentOperationId,
        Guid? userId,
        string eventType,
        string entityName,
        Guid entityId,
        string sourceEndpoint,
        string? sourceScreen,
        string? sourceAction,
        string beforeJson,
        string afterJson,
        string changedFieldsJson)
    {
        OperationId = operationId;
        ParentOperationId = parentOperationId;
        UserId = userId;
        EventType = Guard.NotEmpty(eventType, nameof(eventType));
        EntityName = Guard.NotEmpty(entityName, nameof(entityName));
        EntityId = entityId;
        SourceEndpoint = Guard.NotEmpty(sourceEndpoint, nameof(sourceEndpoint));
        SourceScreen = sourceScreen;
        SourceAction = sourceAction;
        BeforeJson = Guard.NotEmpty(beforeJson, nameof(beforeJson));
        AfterJson = Guard.NotEmpty(afterJson, nameof(afterJson));
        ChangedFieldsJson = Guard.NotEmpty(changedFieldsJson, nameof(changedFieldsJson));
    }
}