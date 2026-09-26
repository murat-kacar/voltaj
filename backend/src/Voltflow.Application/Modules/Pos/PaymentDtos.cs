namespace Voltflow.Application.Dtos;

public sealed record CreatePaymentRequest(Guid CustomerId, decimal Amount, string PaymentMethod, DateOnly PaymentDate);
public sealed record PaymentDto(Guid Id, Guid CustomerId, decimal Amount, string PaymentMethod, DateOnly PaymentDate);
public sealed record AllocatePaymentRequest(Guid PaymentId, Guid InvoiceId, decimal Amount);
public sealed record StageInvoiceRequest(Guid CustomerId, string InvoiceNumber, decimal TotalAmount, DateOnly InvoiceDate);
public sealed record PaymentAllocationDto(Guid PaymentId, Guid InvoiceId, decimal Amount);
public sealed record SalesInvoiceDto(Guid Id, Guid CustomerId, string InvoiceNumber, decimal GrandTotal, decimal PaidAmount, decimal AppliedDepositAmount, decimal RemainingAmount, DateOnly InvoiceDate);

/// <summary>A payment as a row of the list of all payments: carries the customer's name, so the list can be read without opening each customer.</summary>
public sealed record PaymentSummaryDto(Guid Id, Guid CustomerId, string CustomerName, decimal Amount, string PaymentMethod, DateOnly PaymentDate);

/// <summary>An invoice as a row of the list of all invoices, with the customer's name.</summary>
public sealed record SalesInvoiceSummaryDto(Guid Id, Guid CustomerId, string CustomerName, string InvoiceNumber, decimal GrandTotal, decimal PaidAmount, decimal AppliedDepositAmount, decimal RemainingAmount, DateOnly InvoiceDate);
