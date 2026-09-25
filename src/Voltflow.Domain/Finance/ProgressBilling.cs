using Voltflow.Domain.Common;

namespace Voltflow.Domain.Finance;

public sealed class ProgressBilling : Entity
{
    public Guid ServiceId { get; private set; }
    public string BillingNumber { get; private set; } = string.Empty;
    public decimal RequestedAmount { get; private set; }
    public decimal ApprovedAmount { get; private set; }
    public decimal DeductionAmount { get; private set; }
    public decimal NetPayableAmount => ApprovedAmount - DeductionAmount;
    public bool IsApproved { get; private set; }

    private ProgressBilling() { }

    public ProgressBilling(Guid serviceId, string billingNumber, decimal requestedAmount, decimal deductionAmount)
    {
        ServiceId = Guard.AgainstEmptyGuid(serviceId, nameof(serviceId));
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
