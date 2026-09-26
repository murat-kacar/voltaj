using Voltflow.Domain.Common;

namespace Voltflow.Domain.Sales;

public enum QuickSaleStatus
{
    Completed,
    Voided
}

public enum SalePaymentMethod
{
    Cash,
    Card,
    BankTransfer
}

/// <summary>One line the cashier rang up. <see cref="UnitPrice"/> is what the customer pays per unit, VAT included.</summary>
public sealed record QuickSaleLineInput(
    Guid? ProductId,
    string? ProductCode,
    string? Barcode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal DiscountAmount,
    bool TracksStock);

public sealed record QuickSalePaymentInput(SalePaymentMethod Method, decimal Amount, string? Reference);

/// <summary>
/// A sale rung up at the counter: paid on the spot, stock leaves immediately, the customer is optional.
/// All prices are VAT-inclusive and the VAT is extracted per line for the receipt. It is an operational record,
/// not a fiscal document.
/// </summary>
public sealed class QuickSale : Entity
{
    private readonly List<QuickSaleLine> _lines = new();
    private readonly List<QuickSalePayment> _payments = new();

    public string SaleNumber { get; private set; } = string.Empty;
    public DateTime SoldAt { get; private set; }
    public Guid CashierUserId { get; private set; }
    public string CashierName { get; private set; } = string.Empty;
    public Guid ShiftId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public QuickSaleStatus Status { get; private set; }

    /// <summary>Quantity times price over all lines, before any discount.</summary>
    public decimal Subtotal { get; private set; }

    public decimal LineDiscountTotal { get; private set; }
    public decimal ReceiptDiscount { get; private set; }

    /// <summary>What the customer paid for the goods.</summary>
    public decimal GrandTotal { get; private set; }

    /// <summary>The VAT that is already inside <see cref="GrandTotal"/>.</summary>
    public decimal VatTotal { get; private set; }

    public decimal CashTendered { get; private set; }
    public decimal ChangeGiven { get; private set; }
    public string? Note { get; private set; }
    public DateTime? VoidedAt { get; private set; }
    public Guid? VoidedByUserId { get; private set; }
    public string? VoidReason { get; private set; }

    public IReadOnlyCollection<QuickSaleLine> Lines => _lines;
    public IReadOnlyCollection<QuickSalePayment> Payments => _payments;

    private QuickSale() { }

    /// <param name="nextNumber">Called only once every rule has passed, so a rejected sale never takes a receipt number.</param>
    public static QuickSale Create(
        Func<string> nextNumber,
        DateTime soldAt,
        Guid cashierUserId,
        string cashierName,
        Guid shiftId,
        Guid? customerId,
        IReadOnlyList<QuickSaleLineInput> lines,
        decimal receiptDiscount,
        IReadOnlyList<QuickSalePaymentInput> payments,
        string? note)
    {
        if (lines.Count == 0) throw new InvalidOperationException("A sale needs at least one line.");
        Guard.AgainstEmptyGuid(cashierUserId, nameof(cashierUserId));
        Guard.AgainstEmptyGuid(shiftId, nameof(shiftId));

        // 1) the money of every line; nothing exists yet, so a rule that fails leaves no trace
        var gross = new decimal[lines.Count];
        var lineDiscounts = new decimal[lines.Count];
        var nets = new decimal[lines.Count];
        for (var i = 0; i < lines.Count; i++)
        {
            var input = lines[i];
            Guard.NotEmpty(input.Description, nameof(input.Description));
            Guard.AgainstNegativeOrZero(input.Quantity, nameof(input.Quantity));
            Guard.AgainstNegative(input.UnitPrice, nameof(input.UnitPrice));
            Guard.AgainstOutOfRange(input.VatRate, 0, 100, nameof(input.VatRate));
            gross[i] = Money.Round(input.Quantity * input.UnitPrice);
            lineDiscounts[i] = Money.Round(input.DiscountAmount);
            if (lineDiscounts[i] < 0 || lineDiscounts[i] > gross[i])
                throw new InvalidOperationException($"The discount on '{input.Description}' cannot be negative or exceed the line amount.");
            nets[i] = gross[i] - lineDiscounts[i];
        }

        var netSum = nets.Sum();
        receiptDiscount = Money.Round(receiptDiscount);
        if (receiptDiscount < 0 || receiptDiscount > netSum)
            throw new InvalidOperationException("The receipt discount cannot be negative or exceed the amount payable.");

        var shares = SpreadDiscount(receiptDiscount, nets, netSum);
        var totals = new decimal[lines.Count];
        var vats = new decimal[lines.Count];
        for (var i = 0; i < lines.Count; i++)
        {
            totals[i] = nets[i] - shares[i];
            vats[i] = Money.Round(totals[i] * lines[i].VatRate / (100m + lines[i].VatRate));
        }

        var grandTotal = totals.Sum();
        if (grandTotal <= 0) throw new InvalidOperationException("The total of a sale must be greater than zero.");

        // 2) the payments against that total
        var plan = PlanPayments(payments, grandTotal);

        // 3) only now is the receipt number taken and the sale built
        var sale = new QuickSale
        {
            SaleNumber = Guard.NotEmpty(nextNumber(), nameof(nextNumber)),
            SoldAt = soldAt,
            CashierUserId = cashierUserId,
            CashierName = cashierName?.Trim() ?? string.Empty,
            ShiftId = shiftId,
            CustomerId = customerId,
            Status = QuickSaleStatus.Completed,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Subtotal = gross.Sum(),
            LineDiscountTotal = lineDiscounts.Sum(),
            ReceiptDiscount = receiptDiscount,
            GrandTotal = grandTotal,
            VatTotal = vats.Sum(),
            CashTendered = plan.CashTendered,
            ChangeGiven = plan.Change
        };

        for (var i = 0; i < lines.Count; i++)
            sale._lines.Add(new QuickSaleLine(sale.Id, i + 1, lines[i], lineDiscounts[i], shares[i], totals[i], vats[i]));
        foreach (var payment in plan.Payments)
            sale._payments.Add(new QuickSalePayment(sale.Id, payment.Method, payment.Amount, payment.Tendered, payment.Reference));

        return sale;
    }

    /// <summary>Only a sale without returns can be voided, and only while it is still completed.</summary>
    public void Void(Guid voidedByUserId, string reason, DateTime now)
    {
        // Everything is checked before anything changes: a rejected call must leave the sale exactly as it was.
        var cleanReason = Guard.NotEmpty(reason, nameof(reason)).Trim();
        if (Status != QuickSaleStatus.Completed) throw new InvalidOperationException("Only a completed sale can be voided.");
        if (_lines.Any(line => line.ReturnedQuantity > 0)) throw new InvalidOperationException("A sale that has returns cannot be voided.");

        Status = QuickSaleStatus.Voided;
        VoidedAt = now;
        VoidedByUserId = voidedByUserId;
        VoidReason = cleanReason;
        Touch(now);
    }

    /// <summary>Gives the rounding cents to the lines that still have room, so the shares add up to the discount exactly.</summary>
    private static decimal[] SpreadDiscount(decimal discount, decimal[] nets, decimal netSum)
    {
        var shares = new decimal[nets.Length];
        if (discount == 0) return shares;

        for (var i = 0; i < nets.Length; i++)
            shares[i] = Math.Min(nets[i], Money.Round(discount * nets[i] / netSum));

        var left = discount - shares.Sum();
        for (var i = 0; i < nets.Length && left != 0; i++)
        {
            var room = left > 0 ? nets[i] - shares[i] : shares[i];
            var step = Math.Min(Math.Abs(left), room) * Math.Sign(left);
            shares[i] += step;
            left -= step;
        }

        return shares;
    }

    private sealed record PaymentPlan(
        List<(SalePaymentMethod Method, decimal Amount, decimal Tendered, string? Reference)> Payments,
        decimal CashTendered,
        decimal Change);

    private static PaymentPlan PlanPayments(IReadOnlyList<QuickSalePaymentInput> payments, decimal grandTotal)
    {
        if (payments.Count == 0) throw new InvalidOperationException("At least one payment is required.");

        decimal nonCash = 0;
        decimal cash = 0;
        var planned = new List<(SalePaymentMethod Method, decimal Amount, decimal Tendered, string? Reference)>();
        foreach (var payment in payments)
        {
            var amount = Money.Round(payment.Amount);
            if (amount <= 0) throw new InvalidOperationException("Every payment must be greater than zero.");
            if (payment.Method == SalePaymentMethod.Cash)
            {
                cash += amount;
                continue;
            }

            nonCash += amount;
            planned.Add((payment.Method, amount, amount, payment.Reference));
        }

        if (nonCash > grandTotal) throw new InvalidOperationException("Card and transfer payments cannot exceed the total.");
        var cashDue = grandTotal - nonCash;
        if (cash < cashDue) throw new InvalidOperationException("The payments do not cover the total.");
        if (cashDue == 0 && cash > 0) throw new InvalidOperationException("Nothing is due in cash; remove the cash payment.");

        if (cashDue > 0) planned.Add((SalePaymentMethod.Cash, cashDue, cash, null));
        return new PaymentPlan(planned, cash, cash - cashDue);
    }
}

public sealed class QuickSaleLine : Entity
{
    public Guid QuickSaleId { get; private set; }

    /// <summary>The position on the receipt, starting at 1; it keeps the lines in the order they were rung up.</summary>
    public int LineNumber { get; private set; }

    public Guid? ProductId { get; private set; }
    public string? ProductCode { get; private set; }
    public string? Barcode { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;

    /// <summary>Whether stock left when this line was sold; it decides whether a void or a return puts stock back.</summary>
    public bool TracksStock { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal LineDiscount { get; private set; }
    public decimal ReceiptDiscountShare { get; private set; }

    /// <summary>What the customer paid for this line after every discount, VAT included.</summary>
    public decimal LineTotal { get; private set; }

    public decimal VatAmount { get; private set; }
    public decimal ReturnedQuantity { get; private set; }

    private QuickSaleLine() { }

    internal QuickSaleLine(Guid quickSaleId, int lineNumber, QuickSaleLineInput input, decimal lineDiscount, decimal receiptDiscountShare, decimal lineTotal, decimal vatAmount)
    {
        QuickSaleId = quickSaleId;
        LineNumber = lineNumber;
        ProductId = input.ProductId;
        ProductCode = string.IsNullOrWhiteSpace(input.ProductCode) ? null : input.ProductCode.Trim();
        Barcode = string.IsNullOrWhiteSpace(input.Barcode) ? null : input.Barcode.Trim();
        Description = Guard.NotEmpty(input.Description, nameof(input.Description)).Trim();
        Unit = string.IsNullOrWhiteSpace(input.Unit) ? "adet" : input.Unit.Trim();
        TracksStock = input.TracksStock;
        Quantity = input.Quantity;
        UnitPrice = input.UnitPrice;
        VatRate = input.VatRate;
        LineDiscount = lineDiscount;
        ReceiptDiscountShare = receiptDiscountShare;
        LineTotal = lineTotal;
        VatAmount = vatAmount;
    }

    public decimal RemainingQuantity => Quantity - ReturnedQuantity;

    /// <summary>Takes back <paramref name="quantity"/> and returns the refund. The refund is worked out cumulatively, so
    /// returning a line in several parts refunds exactly the line total and never a cent more.</summary>
    internal decimal RegisterReturn(decimal quantity)
    {
        Guard.AgainstNegativeOrZero(quantity, nameof(quantity));
        if (quantity > RemainingQuantity)
            throw new InvalidOperationException($"Cannot return more than was sold of '{Description}'.");

        var refundedBefore = Money.Round(LineTotal * ReturnedQuantity / Quantity);
        ReturnedQuantity += quantity;
        var refundedAfter = Money.Round(LineTotal * ReturnedQuantity / Quantity);
        Touch();
        return refundedAfter - refundedBefore;
    }
}

public sealed class QuickSalePayment : Entity
{
    public Guid QuickSaleId { get; private set; }
    public SalePaymentMethod Method { get; private set; }

    /// <summary>What was applied to the sale.</summary>
    public decimal Amount { get; private set; }

    /// <summary>What the customer handed over. It only differs from <see cref="Amount"/> for cash, where the rest is change.</summary>
    public decimal Tendered { get; private set; }

    public string? Reference { get; private set; }

    private QuickSalePayment() { }

    internal QuickSalePayment(Guid quickSaleId, SalePaymentMethod method, decimal amount, decimal tendered, string? reference)
    {
        QuickSaleId = quickSaleId;
        Method = method;
        Amount = amount;
        Tendered = tendered;
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }
}
