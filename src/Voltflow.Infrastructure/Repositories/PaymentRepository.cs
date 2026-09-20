using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Finance;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly VoltflowDbContext _dbContext;

    public PaymentRepository(VoltflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(CustomerPayment Payment, CustomerLedgerEntry LedgerEntry)> AddPaymentAsync(CustomerPayment payment, CancellationToken ct = default)
    {
        var isRelational = _dbContext.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = isRelational ? await _dbContext.Database.BeginTransactionAsync(ct) : null;
        try
        {
            var balance = await _dbContext.CustomerLedgerEntries
                .Where(x => x.CustomerId == payment.CustomerId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.BalanceAfter)
                .FirstOrDefaultAsync(ct);
            var ledger = new CustomerLedgerEntry(payment.CustomerId, payment.Amount, "CREDIT", balance + payment.Amount, "Customer payment received");
            await _dbContext.CustomerPayments.AddAsync(payment, ct);
            await _dbContext.CustomerLedgerEntries.AddAsync(ledger, ct);
            await _dbContext.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return (payment, ledger);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<CustomerPayment>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
        => await _dbContext.CustomerPayments.Where(x => x.CustomerId == customerId).OrderByDescending(x => x.PaymentDate).ToListAsync(ct);

    public async Task<IReadOnlyList<SalesInvoice>> ListInvoicesByCustomerAsync(Guid customerId, CancellationToken ct = default)
        => await _dbContext.SalesInvoices.Where(x => x.CustomerId == customerId).OrderByDescending(x => x.InvoiceDate).ToListAsync(ct);

    public async Task<PagedResult<CustomerPayment>> ListByCustomerPagedAsync(Guid customerId, int limit, int offset, CancellationToken ct = default)
    {
        var query = _dbContext.CustomerPayments.Where(x => x.CustomerId == customerId).OrderByDescending(x => x.PaymentDate);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<CustomerPayment>(items, total, limit, offset);
    }

    public async Task<PagedResult<SalesInvoice>> ListInvoicesByCustomerPagedAsync(Guid customerId, int limit, int offset, CancellationToken ct = default)
    {
        var query = _dbContext.SalesInvoices.Where(x => x.CustomerId == customerId).OrderByDescending(x => x.InvoiceDate);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<SalesInvoice>(items, total, limit, offset);
    }


    public void StageInvoice(SalesInvoice invoice)
        => _dbContext.SalesInvoices.Add(invoice);

    public async Task<PaymentInvoiceAllocation> AllocateToInvoiceAsync(Guid paymentId, Guid invoiceId, decimal amount, CancellationToken ct = default)
    {
        var isRelational = _dbContext.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = isRelational ? await _dbContext.Database.BeginTransactionAsync(ct) : null;
        try
        {
            if (await _dbContext.PaymentInvoiceAllocations.AnyAsync(x => x.PaymentId == paymentId && x.InvoiceId == invoiceId, ct))
                throw new InvalidOperationException("This payment is already allocated to the invoice.");
            var payment = await _dbContext.CustomerPayments.SingleOrDefaultAsync(x => x.Id == paymentId, ct)
                ?? throw new InvalidOperationException("Payment not found.");
            var invoice = await _dbContext.SalesInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId, ct)
                ?? throw new InvalidOperationException("Invoice not found.");
            payment.Allocate(amount);
            invoice.Allocate(amount);
            var allocation = new PaymentInvoiceAllocation(paymentId, invoiceId, amount);
            _dbContext.PaymentInvoiceAllocations.Add(allocation);
            await _dbContext.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return allocation;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }
}