using Voltflow.Domain.Common;

namespace Voltflow.Domain.WorkOrders;

public sealed class MaintenanceContract : Entity
{
    public Guid CustomerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int FrequencyMonths { get; private set; }
    public DateTime? NextMaintenanceDate { get; private set; }
    public bool IsActive { get; private set; }

    private MaintenanceContract() { }

    public MaintenanceContract(Guid customerId, string title, int frequencyMonths, DateTime firstMaintenanceDate)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer ID required", nameof(customerId));
        if (frequencyMonths <= 0) throw new ArgumentOutOfRangeException(nameof(frequencyMonths));
        
        CustomerId = customerId;
        Title = Guard.NotEmpty(title, nameof(title));
        FrequencyMonths = frequencyMonths;
        NextMaintenanceDate = firstMaintenanceDate;
        IsActive = true;
    }

    public void GenerateWorkOrderCompleted()
    {
        if (!IsActive || !NextMaintenanceDate.HasValue) return;

        NextMaintenanceDate = NextMaintenanceDate.Value.AddMonths(FrequencyMonths);
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
