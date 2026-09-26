namespace Voltflow.Domain.Services;

/// <summary>
/// The unified operational status of a Service (Hizmet).
/// Covers the entire lifecycle from proposal (Draft) to operations (Active) to completion.
/// </summary>
public enum ServiceStatus
{
    /// <summary>Initial proposal phase. Can be freely edited.</summary>
    Draft = 1,

    /// <summary>Proposal sent to customer. Awaiting decision.</summary>
    Issued = 2,

    /// <summary>Service accepted and in progress — in the Active Services Pool.</summary>
    Active = 3,

    /// <summary>Temporarily paused (waiting for materials, access, etc.).</summary>
    OnHold = 4,

    /// <summary>All work done; final invoice has been issued. Terminal.</summary>
    Completed = 5,

    /// <summary>Customer rejected the proposal. Terminal.</summary>
    Rejected = 6,

    /// <summary>Service was cancelled manually. Terminal.</summary>
    Cancelled = 7,
}
