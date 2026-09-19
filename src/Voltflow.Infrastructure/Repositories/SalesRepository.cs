using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Sales;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class SalesRepository : ISalesRepository
{
    private readonly VoltflowDbContext _dbContext;

    public SalesRepository(VoltflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ---- shifts ------------------------------------------------------------------------------------------

    public Task<CashShift?> GetShiftAsync(Guid id, CancellationToken ct = default)
        => _dbContext.CashShifts.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CashShift?> GetOpenShiftAsync(Guid cashierUserId, CancellationToken ct = default)
        => _dbContext.CashShifts.FirstOrDefaultAsync(x => x.CashierUserId == cashierUserId && x.Status == CashShiftStatus.Open, ct);

    public void AddShift(CashShift shift) => _dbContext.CashShifts.Add(shift);

    public async Task<PagedResult<CashShift>> ListShiftsPagedAsync(Guid? cashierUserId, int limit, int offset, CancellationToken ct = default)
    {
        var query = _dbContext.CashShifts.AsNoTracking().AsQueryable();
        if (cashierUserId is { } cashier) query = query.Where(x => x.CashierUserId == cashier);

        var ordered = query.OrderByDescending(x => x.OpenedAt);
        var total = await ordered.CountAsync(ct);
        var items = await ordered.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<CashShift>(items, total, limit, offset);
    }

    public async Task<ShiftTotals> GetShiftTotalsAsync(Guid shiftId, CancellationToken ct = default)
    {
        // A shift rings up hundreds of sales at most, so the light rows are read and added up here.
        var sales = await _dbContext.QuickSales.AsNoTracking()
            .Where(x => x.ShiftId == shiftId)
            .Select(x => new { x.Status, x.GrandTotal, x.LineDiscountTotal, x.ReceiptDiscount, x.VatTotal })
            .ToListAsync(ct);
        var completed = sales.Where(x => x.Status == QuickSaleStatus.Completed).ToList();

        var payments = await _dbContext.QuickSalePayments.AsNoTracking()
            .Where(p => _dbContext.QuickSales.Any(s => s.Id == p.QuickSaleId && s.ShiftId == shiftId && s.Status == QuickSaleStatus.Completed))
            .Select(p => new { p.Method, p.Amount })
            .ToListAsync(ct);

        var lines = await _dbContext.QuickSaleLines.AsNoTracking()
            .Where(l => _dbContext.QuickSales.Any(s => s.Id == l.QuickSaleId && s.ShiftId == shiftId && s.Status == QuickSaleStatus.Completed))
            .Select(l => new { l.VatRate, l.LineTotal, l.VatAmount })
            .ToListAsync(ct);

        var returns = await _dbContext.QuickSaleReturns.AsNoTracking()
            .Where(r => r.ShiftId == shiftId)
            .Select(r => new { r.RefundMethod, r.RefundTotal })
            .ToListAsync(ct);

        return new ShiftTotals(
            completed.Count,
            sales.Count - completed.Count,
            completed.Sum(x => x.GrandTotal),
            completed.Sum(x => x.LineDiscountTotal + x.ReceiptDiscount),
            completed.Sum(x => x.VatTotal),
            payments.GroupBy(x => x.Method).Select(g => new PaymentTotal(g.Key, g.Sum(x => x.Amount))).ToList(),
            lines.GroupBy(x => x.VatRate).OrderBy(g => g.Key).Select(g => new VatBucket(g.Key, g.Sum(x => x.LineTotal), g.Sum(x => x.VatAmount))).ToList(),
            returns.Count,
            returns.Sum(x => x.RefundTotal),
            returns.Where(x => x.RefundMethod == SalePaymentMethod.Cash).Sum(x => x.RefundTotal));
    }

    // ---- sales -------------------------------------------------------------------------------------------

    public Task<QuickSale?> GetSaleAsync(Guid id, CancellationToken ct = default)
        => _dbContext.QuickSales.Include(x => x.Lines).Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == id, ct);

    public void AddSale(QuickSale sale) => _dbContext.QuickSales.Add(sale);

    public async Task<PagedResult<QuickSale>> ListSalesPagedAsync(QuickSaleFilter filter, int limit, int offset, CancellationToken ct = default)
    {
        var query = _dbContext.QuickSales.AsNoTracking().AsQueryable();
        if (filter.CashierUserId is { } cashier) query = query.Where(x => x.CashierUserId == cashier);
        if (filter.CustomerId is { } customer) query = query.Where(x => x.CustomerId == customer);
        if (filter.Status is { } status) query = query.Where(x => x.Status == status);
        if (filter.From is { } from) query = query.Where(x => x.SoldAt >= from);
        if (filter.To is { } to) query = query.Where(x => x.SoldAt < to);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.SaleNumber.ToLower().Contains(term));
        }

        var ordered = query.OrderByDescending(x => x.SoldAt).ThenByDescending(x => x.SaleNumber);
        var total = await ordered.CountAsync(ct);
        var items = await ordered.Include(x => x.Lines).Include(x => x.Payments).Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<QuickSale>(items, total, limit, offset);
    }

    // ---- returns -----------------------------------------------------------------------------------------

    public async Task<IReadOnlyList<QuickSaleReturn>> GetReturnsAsync(Guid quickSaleId, CancellationToken ct = default)
        => await _dbContext.QuickSaleReturns.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.QuickSaleId == quickSaleId)
            .OrderBy(x => x.ReturnedAt).ThenBy(x => x.ReturnNumber)
            .ToListAsync(ct);

    public void AddReturn(QuickSaleReturn saleReturn) => _dbContext.QuickSaleReturns.Add(saleReturn);
}
