using Voltflow.Domain.Services;

namespace Voltflow.Application.Dtos;

// ---- read DTOs ---------------------------------------------------------------

public sealed record ServiceItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    string Kind,
    decimal LineTotal,
    bool IsActive,
    DateTime AddedAt,
    DateTime? RemovedAt,
    string AuditNote);

public sealed record ServiceBillingDto(
    decimal CurrentTotal,
    decimal TotalBilled,
    decimal RemainingLimit,
    decimal DepositPaidAmount,
    decimal RequiredDepositPercentage,
    decimal RequiredDepositAmount);

public sealed record ServiceDto(
    Guid Id,
    Guid CustomerId,
    string Number,
    string Title,
    string? Notes,
    string Status,
    string SubStatus,
    Guid? AssignedUserId,
    Guid? SiteId,
    Guid? AssetId,
    
    DateOnly? ValidUntil,
    DateTime? IssuedAt,
    DateTime? DecidedAt,
    string? RejectionReason,
    bool IsChangeOrder,
    Guid? ParentServiceId,

    ServiceBillingDto Billing,
    IReadOnlyList<ServiceItemDto> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static ServiceDto MapFrom(Service s) => new(
        s.Id, s.CustomerId, s.Number, s.Title, s.Notes,
        s.Status.ToString(), s.SubStatus.ToString(), s.AssignedUserId, s.SiteId, s.AssetId,
        s.ValidUntil, s.IssuedAt, s.DecidedAt, s.RejectionReason,
        s.IsChangeOrder, s.ParentServiceId,
        new ServiceBillingDto(s.CurrentTotal, s.TotalBilled, s.RemainingLimit, s.DepositPaidAmount, s.RequiredDepositPercentage, s.RequiredDepositAmount),
        s.Items.Select(i => new ServiceItemDto(
            i.Id, i.Description, i.Quantity, i.Unit, i.UnitPrice, i.VatRate,
            i.Kind, i.LineTotal, i.IsActive, i.AddedAt, i.RemovedAt, i.AuditNote)).ToList(),
        s.CreatedAt, s.UpdatedAt);
}

public sealed record ServiceSummaryDto(
    Guid Id,
    Guid CustomerId,
    string Number,
    string Title,
    string Status,
    string SubStatus,
    Guid? AssignedUserId,
    decimal CurrentTotal,
    decimal RemainingLimit,
    DateTime CreatedAt)
{
    public static ServiceSummaryDto MapFrom(Service s) => new(
        s.Id, s.CustomerId, s.Number, s.Title,
        s.Status.ToString(), s.SubStatus.ToString(), s.AssignedUserId,
        s.CurrentTotal, s.RemainingLimit, s.CreatedAt);
}

// ---- write requests ----------------------------------------------------------

public sealed record ServiceLineRequest(
    string Description, 
    decimal Quantity, 
    decimal UnitPrice, 
    string? Unit = null, 
    decimal? VatRate = null, 
    string? Kind = null,
    string? AuditNote = null);

public sealed record CreateServiceDraftRequest(
    Guid CustomerId, 
    string Title, 
    string? Notes = null, 
    DateOnly? ValidUntil = null, 
    Guid? SiteId = null, 
    Guid? AssetId = null, 
    List<ServiceLineRequest>? Items = null);

public sealed record UpdateServiceDraftRequest(
    string Title, 
    string? Notes = null, 
    DateOnly? ValidUntil = null, 
    Guid? SiteId = null, 
    Guid? AssetId = null, 
    List<ServiceLineRequest>? Items = null);

public sealed record AcceptServiceRequest(decimal? RequiredDepositPercentage = null);
public sealed record RejectServiceRequest(string Reason);
public sealed record CancelServiceRequest(string? Reason = null);
public sealed record PayServiceDepositRequest(decimal Amount, string PaymentMethod);

public sealed record AssignServiceRequest(Guid AssignedUserId);
public sealed record HoldServiceRequest(string Reason);
public sealed record CompleteServiceRequest(decimal? FinalAmountOverride = null);
public sealed record IssuePartialInvoiceRequest(decimal Amount);
public sealed record UpdateSubStatusRequest(ServiceSubStatus SubStatus);
