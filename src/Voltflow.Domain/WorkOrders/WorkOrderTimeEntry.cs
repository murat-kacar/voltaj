using Voltflow.Domain.Common;

namespace Voltflow.Domain.WorkOrders;

public sealed class WorkOrderTimeEntry
{
    public Guid Id { get; private set; }
    public DateTime CheckInTime { get; private set; }
    public DateTime? CheckOutTime { get; private set; }
    public TimeSpan? Duration => CheckOutTime.HasValue ? CheckOutTime.Value - CheckInTime : null;
    public string? Notes { get; private set; }

    private WorkOrderTimeEntry() { } // EF Core

    internal WorkOrderTimeEntry(DateTime checkInTime, string? notes = null)
    {
        Id = Guid.NewGuid();
        CheckInTime = checkInTime;
        Notes = notes;
    }

    internal void CheckOut(DateTime checkOutTime, string? notes = null)
    {
        if (checkOutTime < CheckInTime)
            throw new InvalidOperationException("CheckOut time cannot be earlier than CheckIn time.");
            
        CheckOutTime = checkOutTime;
        if (notes != null)
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes : $"{Notes}\n{notes}";
        }
    }
}
