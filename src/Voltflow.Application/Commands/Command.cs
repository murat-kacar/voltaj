namespace Voltflow.Application.Commands;

/// <summary>
/// M11: the immutable, verbatim record of what was asked for, captured at the API boundary before
/// any domain logic runs. Recorded to the audit log as-is - downstream logic must not derive a new
/// Command from re-fetched state.
/// </summary>
public sealed record Command(
    Guid CommandId,
    Guid? ParentCommandId,
    Guid? ActorUserId,
    IReadOnlyCollection<string> ActorRoles,
    string TriggerSource,
    string CommandType,
    string PayloadJson,
    DateTime Timestamp);
