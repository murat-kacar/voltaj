using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Finance;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly ICustomerService _customerService;

    public PaymentService(IPaymentRepository repository, ICustomerService customerService)
    {
        _repository = repository;
        _customerService = customerService;
    }

    public async Task<Result<PaymentDto>> CreateAsync(CreatePaymentRequest request, CancellationToken ct = default)
    {
        if (request.CustomerId == Guid.Empty) return Result<PaymentDto>.Fail("CustomerId is required.");
        if (request.Amount <= 0) return Result<PaymentDto>.Fail("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.PaymentMethod)) return Result<PaymentDto>.Fail("PaymentMethod is required.");

        var customer = await _customerService.GetByIdAsync(request.CustomerId, ct);
        if (!customer.IsSuccess) return Result<PaymentDto>.Fail("Customer not found.");

        var payment = new CustomerPayment(request.CustomerId, request.Amount, request.PaymentMethod.Trim(), request.PaymentDate);
        var result = await _repository.AddPaymentAsync(payment, ct);
        return Result<PaymentDto>.Ok(Map(result.Payment));
    }

    public async Task<Result<IReadOnlyList<PaymentDto>>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var payments = await _repository.ListByCustomerAsync(customerId, ct);
        return Result<IReadOnlyList<PaymentDto>>.Ok(payments.Select(Map).ToList());
    }

    public async Task<Result<IReadOnlyList<SalesInvoiceDto>>> ListInvoicesByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var invoices = await _repository.ListInvoicesByCustomerAsync(customerId, ct);
        return Result<IReadOnlyList<SalesInvoiceDto>>.Ok(invoices.Select(MapInvoice).ToList());
    }

    public async Task<Result<PaymentAllocationDto>> AllocateToInvoiceAsync(AllocatePaymentRequest request, CancellationToken ct = default)
    {
        if (request.PaymentId == Guid.Empty || request.InvoiceId == Guid.Empty)
            return Result<PaymentAllocationDto>.Fail("PaymentId and InvoiceId are required.");
        if (request.Amount <= 0) return Result<PaymentAllocationDto>.Fail("Amount must be greater than zero.");
        try
        {
            var allocation = await _repository.AllocateToInvoiceAsync(request.PaymentId, request.InvoiceId, request.Amount, ct);
            return Result<PaymentAllocationDto>.Ok(new PaymentAllocationDto(allocation.PaymentId, allocation.InvoiceId, allocation.Amount));
        }
        catch (InvalidOperationException exception)
        {
            return Result<PaymentAllocationDto>.Fail(exception.Message);
        }
    }

    private static PaymentDto Map(CustomerPayment payment) =>
        new(payment.Id, payment.CustomerId, payment.Amount, payment.PaymentMethod, payment.PaymentDate);

    private static SalesInvoiceDto MapInvoice(SalesInvoice invoice) =>
        new(invoice.Id, invoice.CustomerId, invoice.InvoiceNumber, invoice.GrandTotal, invoice.PaidAmount, invoice.AppliedDepositAmount, invoice.RemainingAmount, invoice.InvoiceDate);
}