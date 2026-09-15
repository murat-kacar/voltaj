using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentDto>> CreateAsync(CreatePaymentRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PaymentDto>>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SalesInvoiceDto>>> ListInvoicesByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<PaymentAllocationDto>> AllocateToInvoiceAsync(AllocatePaymentRequest request, CancellationToken ct = default);
}