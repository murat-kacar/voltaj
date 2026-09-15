using Voltflow.Domain.Common;

namespace Voltflow.Domain.Finance;

public sealed class CustomerPayment : Entity
{
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;
    public DateOnly PaymentDate { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public decimal UnallocatedAmount => Amount - AllocatedAmount;

    private CustomerPayment() { }

    public CustomerPayment(Guid customerId, decimal amount, string paymentMethod, DateOnly paymentDate)
    {
        CustomerId = Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Amount = Guard.AgainstNegativeOrZero(amount, nameof(amount));
        PaymentMethod = Guard.NotEmpty(paymentMethod, nameof(paymentMethod));
        PaymentDate = paymentDate;
    }

    public void Allocate(decimal amount)
    {
        if (amount <= 0 || amount > UnallocatedAmount)
            throw new InvalidOperationException("Payment allocation exceeds the unallocated amount.");
        AllocatedAmount += amount;
        Touch();
    }
}

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

public sealed class PaymentInvoiceAllocation : Entity
{
    public Guid PaymentId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }

    private PaymentInvoiceAllocation() { }

    public PaymentInvoiceAllocation(Guid paymentId, Guid invoiceId, decimal amount)
    {
        PaymentId = Guard.AgainstEmptyGuid(paymentId, nameof(paymentId));
        InvoiceId = Guard.AgainstEmptyGuid(invoiceId, nameof(invoiceId));
        Amount = Guard.AgainstNegativeOrZero(amount, nameof(amount));
    }
}

public sealed class ProgressBilling : Entity
{
    public Guid ProjectId { get; private set; }
    public string BillingNumber { get; private set; } = string.Empty;
    public decimal RequestedAmount { get; private set; }
    public decimal ApprovedAmount { get; private set; }
    public decimal DeductionAmount { get; private set; }
    public decimal NetPayableAmount => ApprovedAmount - DeductionAmount;
    public bool IsApproved { get; private set; }

    private ProgressBilling() { }

    public ProgressBilling(Guid projectId, string billingNumber, decimal requestedAmount, decimal deductionAmount)
    {
        ProjectId = Guard.AgainstEmptyGuid(projectId, nameof(projectId));
        RequestedAmount = Guard.AgainstNegative(requestedAmount, nameof(requestedAmount));
        DeductionAmount = Guard.AgainstNegative(deductionAmount, nameof(deductionAmount));
        BillingNumber = Guard.NotEmpty(billingNumber, nameof(billingNumber));
    }

    public void Approve(decimal approvedAmount)
    {
        if (approvedAmount < 0 || approvedAmount < DeductionAmount)
            throw new InvalidOperationException("Approved amount cannot be lower than deduction.");
        ApprovedAmount = approvedAmount;
        IsApproved = true;
        Touch();
    }
}

public sealed class CustomerLedgerEntry : Entity
{
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Direction { get; private set; } = string.Empty;
    public decimal BalanceAfter { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private CustomerLedgerEntry() { }

    public CustomerLedgerEntry(Guid customerId, decimal amount, string direction, decimal balanceAfter, string description)
    {
        CustomerId = Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Amount = Guard.AgainstNegativeOrZero(amount, nameof(amount));
        BalanceAfter = Guard.AgainstNegative(balanceAfter, nameof(balanceAfter));
        Direction = Guard.NotEmpty(direction, nameof(direction));
        Description = description ?? string.Empty;
    }
}