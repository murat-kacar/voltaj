using Voltflow.Domain.Common;

namespace Voltflow.Domain.Sales;

public enum CashShiftStatus
{
    Open,
    Closed
}

/// <summary>A cashier's shift at the till: opened with a starting float, closed with a count of the cash in the drawer.</summary>
public sealed class CashShift : Entity
{
    public Guid CashierUserId { get; private set; }
    public string CashierName { get; private set; } = string.Empty;
    public DateTime OpenedAt { get; private set; }
    public decimal OpeningCash { get; private set; }
    public CashShiftStatus Status { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public Guid? ClosedByUserId { get; private set; }

    /// <summary>What was counted in the drawer at closing.</summary>
    public decimal? CountedCash { get; private set; }

    /// <summary>What the drawer should hold: the float plus cash sales minus cash refunds.</summary>
    public decimal? ExpectedCash { get; private set; }

    /// <summary>Counted minus expected: negative means the drawer is short.</summary>
    public decimal? CashDifference { get; private set; }

    public string? Note { get; private set; }

    private CashShift() { }

    public static CashShift Open(Guid cashierUserId, string cashierName, decimal openingCash, DateTime now) => new()
    {
        CashierUserId = Guard.AgainstEmptyGuid(cashierUserId, nameof(cashierUserId)),
        CashierName = cashierName?.Trim() ?? string.Empty,
        OpeningCash = Money.Round(Guard.AgainstNegative(openingCash, nameof(openingCash))),
        OpenedAt = now,
        Status = CashShiftStatus.Open
    };

    public void Close(Guid closedByUserId, decimal countedCash, decimal expectedCash, string? note, DateTime now)
    {
        if (Status != CashShiftStatus.Open) throw new InvalidOperationException("The shift is already closed.");

        CountedCash = Money.Round(Guard.AgainstNegative(countedCash, nameof(countedCash)));
        ExpectedCash = Money.Round(expectedCash);
        CashDifference = CountedCash - ExpectedCash;
        ClosedByUserId = closedByUserId;
        ClosedAt = now;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Status = CashShiftStatus.Closed;
        Touch(now);
    }
}
