using Voltflow.Application.Common;
using Voltflow.Domain.Sales;

namespace Voltflow.Application.Interfaces;

public sealed record QuickSaleFilter(string? Search, QuickSaleStatus? Status, DateTime? From, DateTime? To, Guid? CashierUserId);

public sealed record PaymentTotal(SalePaymentMethod Method, decimal Amount);

public sealed record VatBucket(decimal Rate, decimal Gross, decimal Vat);

/// <summary>What one shift has rung up so far. Sales and returns are attached to a shift and a closed shift takes no more,
/// so once it is closed these figures no longer change.</summary>
public sealed record ShiftTotals(
    int SaleCount,
    int VoidedCount,
    decimal SalesTotal,
    decimal DiscountTotal,
    decimal VatTotal,
    IReadOnlyList<PaymentTotal> Payments,
    IReadOnlyList<VatBucket> VatBuckets,
    int ReturnCount,
    decimal ReturnTotal,
    decimal CashRefunds)
{
    public decimal PaidBy(SalePaymentMethod method) => Payments.Where(payment => payment.Method == method).Sum(payment => payment.Amount);
}

/// <summary>
/// Everything the counter writes: sales, returns, shifts and the running numbers. The methods that change data do not save;
/// the service commits once, so a sale, its stock movements and its receipt number land together or not at all.
/// </summary>
public interface ISalesRepository
{
    // running numbers
    Task<DocumentCounter?> FindCounterAsync(string key, CancellationToken ct = default);
    DocumentCounter AddCounter(string key);

    // shifts
    Task<CashShift?> GetShiftAsync(Guid id, CancellationToken ct = default);
    Task<CashShift?> GetOpenShiftAsync(Guid cashierUserId, CancellationToken ct = default);
    void AddShift(CashShift shift);
    Task<PagedResult<CashShift>> ListShiftsPagedAsync(Guid? cashierUserId, int limit, int offset, CancellationToken ct = default);
    Task<ShiftTotals> GetShiftTotalsAsync(Guid shiftId, CancellationToken ct = default);

    // sales
    Task<QuickSale?> GetSaleAsync(Guid id, CancellationToken ct = default);
    void AddSale(QuickSale sale);
    Task<PagedResult<QuickSale>> ListSalesPagedAsync(QuickSaleFilter filter, int limit, int offset, CancellationToken ct = default);

    // returns
    Task<IReadOnlyList<QuickSaleReturn>> GetReturnsAsync(Guid quickSaleId, CancellationToken ct = default);
    void AddReturn(QuickSaleReturn saleReturn);
}
