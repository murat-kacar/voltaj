using Voltflow.Domain.Common;

namespace Voltflow.Domain.Sales;

/// <summary>Goods taken back after a sale. The refund leaves the till of the shift that processes the return.</summary>
public sealed class QuickSaleReturn : Entity
{
    private readonly List<QuickSaleReturnLine> _lines = new();

    public string ReturnNumber { get; private set; } = string.Empty;
    public Guid QuickSaleId { get; private set; }
    public string SaleNumber { get; private set; } = string.Empty;
    public DateTime ReturnedAt { get; private set; }
    public Guid CashierUserId { get; private set; }
    public string CashierName { get; private set; } = string.Empty;
    public Guid ShiftId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public SalePaymentMethod RefundMethod { get; private set; }
    public decimal RefundTotal { get; private set; }

    public IReadOnlyCollection<QuickSaleReturnLine> Lines => _lines;

    private QuickSaleReturn() { }

    /// <summary>Records the return and marks the returned quantities on the sale's lines.</summary>
    /// <param name="nextNumber">Called only once every rule has passed, so a rejected return never takes a number.</param>
    public static QuickSaleReturn Create(
        Func<string> nextNumber,
        QuickSale sale,
        DateTime returnedAt,
        Guid cashierUserId,
        string cashierName,
        Guid shiftId,
        string reason,
        SalePaymentMethod refundMethod,
        IReadOnlyList<(Guid LineId, decimal Quantity)> items)
    {
        if (sale.Status != QuickSaleStatus.Completed) throw new InvalidOperationException("Only a completed sale can have returns.");
        if (items.Count == 0) throw new InvalidOperationException("Choose at least one line to return.");
        if (items.Select(item => item.LineId).Distinct().Count() != items.Count)
            throw new InvalidOperationException("A line can appear only once in a return.");
        var cleanReason = Guard.NotEmpty(reason, nameof(reason)).Trim();
        Guard.AgainstEmptyGuid(cashierUserId, nameof(cashierUserId));
        Guard.AgainstEmptyGuid(shiftId, nameof(shiftId));

        // Check every item first: the lines of the sale are tracked, so a failure half-way through must not
        // have changed the ones before it.
        var planned = new List<(QuickSaleLine Line, decimal Quantity)>();
        foreach (var (lineId, quantity) in items)
        {
            var line = sale.Lines.FirstOrDefault(candidate => candidate.Id == lineId)
                ?? throw new InvalidOperationException("The line does not belong to this sale.");
            Guard.AgainstNegativeOrZero(quantity, nameof(quantity));
            if (quantity > line.RemainingQuantity)
                throw new InvalidOperationException($"Cannot return more than was sold of '{line.Description}'.");
            planned.Add((line, quantity));
        }

        var result = new QuickSaleReturn
        {
            ReturnNumber = Guard.NotEmpty(nextNumber(), nameof(nextNumber)),
            QuickSaleId = sale.Id,
            SaleNumber = sale.SaleNumber,
            ReturnedAt = returnedAt,
            CashierUserId = cashierUserId,
            CashierName = cashierName?.Trim() ?? string.Empty,
            ShiftId = shiftId,
            Reason = cleanReason,
            RefundMethod = refundMethod
        };

        foreach (var (line, quantity) in planned)
        {
            var refund = line.RegisterReturn(quantity);
            result._lines.Add(new QuickSaleReturnLine(result.Id, line, quantity, refund));
        }

        result.RefundTotal = result._lines.Sum(x => x.RefundAmount);
        return result;
    }
}

public sealed class QuickSaleReturnLine : Entity
{
    public Guid QuickSaleReturnId { get; private set; }
    public Guid QuickSaleLineId { get; private set; }
    public string? ProductCode { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool TracksStock { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal RefundAmount { get; private set; }

    private QuickSaleReturnLine() { }

    internal QuickSaleReturnLine(Guid quickSaleReturnId, QuickSaleLine line, decimal quantity, decimal refundAmount)
    {
        QuickSaleReturnId = quickSaleReturnId;
        QuickSaleLineId = line.Id;
        ProductCode = line.ProductCode;
        Description = line.Description;
        TracksStock = line.TracksStock;
        Quantity = quantity;
        RefundAmount = refundAmount;
    }
}
