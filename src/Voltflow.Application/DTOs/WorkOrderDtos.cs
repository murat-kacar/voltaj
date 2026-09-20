namespace Voltflow.Application.Dtos;

public sealed record CreateWorkOrderRequest(Guid CustomerId, string Title);
public sealed record AssignWorkOrderRequest(Guid EmployeeUserId);
public sealed record StartWorkOrderRequest(DateTime? TargetCompletionDate = null);
public sealed record CheckInWorkOrderRequest(string? Notes = null);
public sealed record CheckOutWorkOrderRequest(string? Notes = null);
public sealed record HoldWorkOrderRequest(string Reason);
public sealed record CancelWorkOrderRequest(string Reason);
public sealed record ReportNoShowRequest(string Reason);
public sealed record CompleteWorkOrderRequest(string? SignatureData, string? ProofOfWorkPhotoUrl);
public sealed record AddMaterialToWorkOrderRequest(string Description, decimal Quantity, decimal UnitPrice);
public sealed record TimeEntryDto(DateTime CheckInTime, DateTime? CheckOutTime, string? Notes);
public sealed record WorkOrderItemDto(Guid Id, string Description, decimal Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record WorkOrderDto(
    Guid Id, Guid CustomerId, Guid? AssignedUserId, string Number, string Title,
    decimal Total, string Status,
    bool IsSafetyChecklistCompleted,
    string? HoldReason, string? CancellationReason, DateTime? TargetCompletionDate,
    Guid? SourceQuoteId, Guid? SiteId, Guid? AssetId,
    string? SignatureData, string? ProofOfWorkPhotoUrl,
    IReadOnlyList<TimeEntryDto> TimeEntries,
    IReadOnlyList<WorkOrderItemDto> Items)
{
    public static WorkOrderDto MapFrom(Voltflow.Domain.WorkOrders.WorkOrder order) => new(
        order.Id, order.CustomerId, order.AssignedUserId, order.Number, order.Title, order.Total,
        order.Status.ToString(),
        order.IsSafetyChecklistCompleted,
        order.HoldReason, order.CancellationReason, order.TargetCompletionDate,
        order.SourceQuoteId, order.SiteId, order.AssetId,
        order.SignatureData, order.ProofOfWorkPhotoUrl,
        order.TimeEntries.Select(x => new TimeEntryDto(x.CheckInTime, x.CheckOutTime, x.Notes)).ToList(),
        order.Items.Select(x => new WorkOrderItemDto(x.Id, x.Description, x.Quantity, x.UnitPrice, x.LineTotal)).ToList());
}
