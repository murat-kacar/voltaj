using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentDto>> CreateAsync(CreatePaymentRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<PaymentDto>>> ListByCustomerAsync(Guid customerId, int? limit = null, int? offset = null, CancellationToken ct = default);
    Task<Result<PagedResult<SalesInvoiceDto>>> ListInvoicesByCustomerAsync(Guid customerId, int? limit = null, int? offset = null, CancellationToken ct = default);
    Task<Result<PaymentAllocationDto>> AllocateToInvoiceAsync(AllocatePaymentRequest request, CancellationToken ct = default);
}