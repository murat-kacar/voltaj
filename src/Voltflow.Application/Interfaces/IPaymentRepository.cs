using Voltflow.Application.Common;
using Voltflow.Domain.Finance;

namespace Voltflow.Application.Interfaces;

public interface IPaymentRepository
{
    Task<(CustomerPayment Payment, CustomerLedgerEntry LedgerEntry)> AddPaymentAsync(CustomerPayment payment, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerPayment>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<SalesInvoice>> ListInvoicesByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<PaymentInvoiceAllocation> AllocateToInvoiceAsync(Guid paymentId, Guid invoiceId, decimal amount, CancellationToken ct = default);
    Task<PagedResult<CustomerPayment>> ListByCustomerPagedAsync(Guid customerId, int limit, int offset, CancellationToken ct = default);
    Task<PagedResult<SalesInvoice>> ListInvoicesByCustomerPagedAsync(Guid customerId, int limit, int offset, CancellationToken ct = default);
    /// <summary>One page of the payments, newest first; <paramref name="customerId"/> null means every customer's.</summary>
    Task<PagedResult<CustomerPayment>> ListPagedAsync(Guid? customerId, int limit, int offset, CancellationToken ct = default);

    /// <summary>One page of the invoices, newest first; <paramref name="customerId"/> null means every customer's.</summary>
    Task<PagedResult<SalesInvoice>> ListInvoicesPagedAsync(Guid? customerId, int limit, int offset, CancellationToken ct = default);

    void StageInvoice(SalesInvoice invoice);
}
