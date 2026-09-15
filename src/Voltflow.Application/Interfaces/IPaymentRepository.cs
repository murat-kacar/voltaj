using Voltflow.Domain.Finance;

namespace Voltflow.Application.Interfaces;

public interface IPaymentRepository
{
    Task<(CustomerPayment Payment, CustomerLedgerEntry LedgerEntry)> AddPaymentAsync(CustomerPayment payment, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerPayment>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<SalesInvoice>> ListInvoicesByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<PaymentInvoiceAllocation> AllocateToInvoiceAsync(Guid paymentId, Guid invoiceId, decimal amount, CancellationToken ct = default);
}
