using Voltflow.Domain.Common;

namespace Voltflow.Domain.Finance;

public enum InvoiceType
{
    Standard,
    CreditNote
}

public sealed class SalesInvoice : Entity
{
    public Guid CustomerId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public decimal GrandTotal { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal AppliedDepositAmount { get; private set; }
    public decimal RemainingAmount => GrandTotal - PaidAmount - AppliedDepositAmount;
    public DateOnly InvoiceDate { get; private set; }
    public InvoiceType Type { get; private set; }

    private SalesInvoice() { }

    public SalesInvoice(Guid customerId, string invoiceNumber, decimal grandTotal, DateOnly invoiceDate, InvoiceType type = InvoiceType.Standard, decimal appliedDepositAmount = 0)
    {
        CustomerId = Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        GrandTotal = Guard.AgainstNegativeOrZero(grandTotal, nameof(grandTotal));
        AppliedDepositAmount = Guard.AgainstOutOfRange(appliedDepositAmount, 0, grandTotal, nameof(appliedDepositAmount));
        
        InvoiceNumber = Guard.NotEmpty(invoiceNumber, nameof(invoiceNumber));
        InvoiceDate = invoiceDate;
        Type = type;
    }

    public void Allocate(decimal amount)
    {
        if (amount <= 0 || amount > RemainingAmount)
            throw new InvalidOperationException("Invoice allocation exceeds the remaining invoice amount.");
        PaidAmount += amount;
        Touch();
    }
}
