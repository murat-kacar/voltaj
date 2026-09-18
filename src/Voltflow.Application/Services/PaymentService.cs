using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Finance;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly ICustomerService _customerService;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;

    public PaymentService(IPaymentRepository repository, ICustomerService customerService, ICommandJournal commandJournal, IOperationContext operationContext)
    {
        _repository = repository;
        _customerService = customerService;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
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
        // AddPaymentAsync saves internally - own follow-up transaction, not piggybacked.
        await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: true, errorCode: null, ct);
        return Result<PaymentDto>.Ok(Map(result.Payment));
    }

    public async Task<Result<PagedResult<PaymentDto>>> ListByCustomerAsync(Guid customerId, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var page = await _repository.ListByCustomerPagedAsync(
            customerId, PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        return Result<PagedResult<PaymentDto>>.Ok(page.Map(Map));
    }

    public async Task<Result<PagedResult<SalesInvoiceDto>>> ListInvoicesByCustomerAsync(Guid customerId, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var page = await _repository.ListInvoicesByCustomerPagedAsync(
            customerId, PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        return Result<PagedResult<SalesInvoiceDto>>.Ok(page.Map(MapInvoice));
    }

    public async Task<Result<PaymentAllocationDto>> AllocateToInvoiceAsync(AllocatePaymentRequest request, CancellationToken ct = default)
    {
        if (request.PaymentId == Guid.Empty || request.InvoiceId == Guid.Empty)
            return Result<PaymentAllocationDto>.Fail("PaymentId and InvoiceId are required.");
        if (request.Amount <= 0) return Result<PaymentAllocationDto>.Fail("Amount must be greater than zero.");
        try
        {
            var allocation = await _repository.AllocateToInvoiceAsync(request.PaymentId, request.InvoiceId, request.Amount, ct);
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: true, errorCode: null, ct);
            return Result<PaymentAllocationDto>.Ok(new PaymentAllocationDto(allocation.PaymentId, allocation.InvoiceId, allocation.Amount));
        }
        catch (InvalidOperationException exception)
        {
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, "DOMAIN_VALIDATION_FAILED", ct);
            return Result<PaymentAllocationDto>.Fail(exception.Message);
        }
    }

    private static PaymentDto Map(CustomerPayment payment) =>
        new(payment.Id, payment.CustomerId, payment.Amount, payment.PaymentMethod, payment.PaymentDate);

    private static SalesInvoiceDto MapInvoice(SalesInvoice invoice) =>
        new(invoice.Id, invoice.CustomerId, invoice.InvoiceNumber, invoice.GrandTotal, invoice.PaidAmount, invoice.AppliedDepositAmount, invoice.RemainingAmount, invoice.InvoiceDate);
}