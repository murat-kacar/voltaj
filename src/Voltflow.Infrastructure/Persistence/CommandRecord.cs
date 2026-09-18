using Voltflow.Domain.Common;

namespace Voltflow.Infrastructure.Persistence;

public enum CommandStatus
{
    Pending,
    Completed,
    Failed
}

public sealed class CommandRecord : Entity
{
    public Guid CommandId { get; private set; }
    public Guid? ParentCommandId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorRoles { get; private set; }
    public string TriggerSource { get; private set; } = string.Empty;
    public string CommandType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public CommandStatus Status { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private CommandRecord() { }

    public CommandRecord(
        Guid commandId,
        Guid? parentCommandId,
        Guid? actorUserId,
        string? actorRoles,
        string triggerSource,
        string commandType,
        string payloadJson)
    {
        CommandId = commandId;
        ParentCommandId = parentCommandId;
        ActorUserId = actorUserId;
        ActorRoles = actorRoles;
        TriggerSource = Guard.NotEmpty(triggerSource, nameof(triggerSource));
        CommandType = Guard.NotEmpty(commandType, nameof(commandType));
        PayloadJson = Guard.NotEmpty(payloadJson, nameof(payloadJson));
        Status = CommandStatus.Pending;
    }

    public void Resolve(bool success, string? errorCode)
    {
        if (Status != CommandStatus.Pending) return;
        Status = success ? CommandStatus.Completed : CommandStatus.Failed;
        ErrorCode = errorCode;
        ResolvedAt = DateTime.UtcNow;
        Touch();
    }
}
