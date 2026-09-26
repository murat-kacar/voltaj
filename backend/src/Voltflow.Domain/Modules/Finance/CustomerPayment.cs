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
