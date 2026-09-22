using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentDto>> CreateAsync(CreatePaymentRequest request, CancellationToken ct = default);
    Task<Result<PaymentDto>> RefundAsync(Guid customerId, decimal amount, string originalPaymentMethod, string reason, CancellationToken ct = default);
    Task<Result<PagedResult<PaymentDto>>> ListByCustomerAsync(Guid customerId, int? limit = null, int? offset = null, CancellationToken ct = default);
    Task<Result<PagedResult<SalesInvoiceDto>>> ListInvoicesByCustomerAsync(Guid customerId, int? limit = null, int? offset = null, CancellationToken ct = default);

    /// <param name="customerId">Narrows the list to one customer; null lists everyone's.</param>
    Task<Result<PagedResult<PaymentSummaryDto>>> ListAsync(Guid? customerId = null, int? limit = null, int? offset = null, CancellationToken ct = default);

    /// <param name="customerId">Narrows the list to one customer; null lists everyone's.</param>
    Task<Result<PagedResult<SalesInvoiceSummaryDto>>> ListInvoicesAsync(Guid? customerId = null, int? limit = null, int? offset = null, CancellationToken ct = default);

    Task<Result<PaymentAllocationDto>> AllocateToInvoiceAsync(AllocatePaymentRequest request, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> StageInvoiceAsync(StageInvoiceRequest request, CancellationToken ct = default);
}