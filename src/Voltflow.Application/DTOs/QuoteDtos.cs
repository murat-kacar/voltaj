namespace Voltflow.Application.Dtos;

public sealed record CreateQuoteRequest(Guid CustomerId, string Title);
public sealed record AddQuoteItemRequest(string Description, decimal Quantity, decimal UnitPrice);
public sealed record RejectQuoteRequest(string Reason);
public sealed record AcceptQuoteRequest(decimal? RequiredDepositPercentage = null);
public sealed record PayQuoteDepositRequest(decimal Amount);
public sealed record QuoteDto(
    Guid Id,
    Guid CustomerId,
    string Number,
    string Title,
    decimal Total,
    string State,
    string? RejectionReason = null,
    decimal RequiredDepositPercentage = 0,
    decimal DepositPaidAmount = 0);
