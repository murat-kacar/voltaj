namespace Voltflow.Application.Dtos;

public sealed record CreatePaymentRequest(Guid CustomerId, decimal Amount, string PaymentMethod, DateOnly PaymentDate);
public sealed record PaymentDto(Guid Id, Guid CustomerId, decimal Amount, string PaymentMethod, DateOnly PaymentDate);
public sealed record AllocatePaymentRequest(Guid PaymentId, Guid InvoiceId, decimal Amount);
public sealed record PaymentAllocationDto(Guid PaymentId, Guid InvoiceId, decimal Amount);
public sealed record SalesInvoiceDto(Guid Id, Guid CustomerId, string InvoiceNumber, decimal GrandTotal, decimal PaidAmount, decimal AppliedDepositAmount, decimal RemainingAmount, DateOnly InvoiceDate);
