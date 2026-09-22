namespace Voltflow.Application.Dtos;

/// <param name="Unit">Blank means "adet".</param>
/// <param name="VatRate">Blank means the usual 20%.</param>
/// <param name="Kind">Material, Labor or Service; blank means Service.</param>
public sealed record QuoteLineRequest(string Description, decimal Quantity, decimal UnitPrice, string? Unit = null, decimal? VatRate = null, string? Kind = null);

/// <param name="ValidUntil">The last day the customer can accept; blank means 15 days from the day the quote is issued.</param>
public sealed record CreateQuoteRequest(
    Guid CustomerId,
    string Title,
    string? Notes = null,
    DateOnly? ValidUntil = null,
    Guid? SiteId = null,
    Guid? AssetId = null,
    IReadOnlyList<QuoteLineRequest>? Items = null);

/// <summary>Changes a draft: its details and the whole list of its lines.</summary>
public sealed record UpdateQuoteRequest(
    string Title, string? Notes, DateOnly? ValidUntil, Guid? SiteId, Guid? AssetId, IReadOnlyList<QuoteLineRequest> Items);

public sealed record RejectQuoteRequest(string Reason);
public sealed record AcceptQuoteRequest(decimal? RequiredDepositPercentage = null);
public sealed record PayQuoteDepositRequest(decimal Amount, string PaymentMethod = "Cash");

public sealed record QuoteLineDto(
    Guid Id,
    int LineNumber,
    string Kind,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal,
    decimal VatAmount);

/// <summary>A row of the quote list.</summary>
public sealed record QuoteSummaryDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string Number,
    string Title,
    string State,
    decimal Total,
    DateOnly? ValidUntil,
    DateTime CreatedAt,
    decimal RequiredDepositAmount,
    decimal DepositPaidAmount);

/// <summary>A whole quote, with everything the screen and the printed document need. Prices are what the customer pays, VAT included.</summary>
public sealed record QuoteDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    string CustomerTaxNumber,
    string Number,
    string Title,
    string? Notes,
    string State,
    decimal Total,
    decimal VatTotal,
    DateTime CreatedAt,
    DateTime? IssuedAt,
    DateOnly? ValidUntil,
    DateTime? DecidedAt,
    string? RejectionReason,
    decimal RequiredDepositPercentage,
    decimal RequiredDepositAmount,
    decimal DepositPaidAmount,
    Guid? SiteId,
    string? SiteName,
    string? SiteAddress,
    Guid? AssetId,
    string? AssetName,
    bool IsChangeOrder,
    Guid? ParentWorkOrderId,
    Guid? WorkOrderId,
    string? WorkOrderNumber,
    IReadOnlyList<QuoteLineDto> Items);
