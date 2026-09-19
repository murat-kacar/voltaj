using Voltflow.Domain.Common;

namespace Voltflow.Domain.WorkOrders;

public sealed class WorkOrder : Entity
{
    public Guid CustomerId { get; private set; }
    public Guid? SourceQuoteId { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public WorkOrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public string? SignatureData { get; private set; }
    public string? ProofOfWorkPhotoUrl { get; private set; }
    public string? HoldReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? TargetCompletionDate { get; private set; }
    
    public Guid? SiteId { get; private set; }
    public Guid? AssetId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? PhaseId { get; private set; }
    public Guid? ParentWorkOrderId { get; private set; }
    public bool IsSafetyChecklistCompleted { get; private set; }

    private readonly List<WorkOrderItem> _items = new();
    public IReadOnlyCollection<WorkOrderItem> Items => _items.AsReadOnly();

    private readonly List<WorkOrderTimeEntry> _timeEntries = new();
    public IReadOnlyCollection<WorkOrderTimeEntry> TimeEntries => _timeEntries.AsReadOnly();

    private WorkOrder() { }

    public WorkOrder(Guid customerId, string title)
    {
        CustomerId = Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Title = Guard.NotEmpty(title, nameof(title));
        Number = $"WO-{DateTime.UtcNow:yyyyMMddHHmmss}";
        Status = WorkOrderStatus.Open;
    }

    public void AddItem(string description, decimal quantity, decimal unitPrice)
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Invoiced or WorkOrderStatus.Cancelled)
            throw new InvalidOperationException("Materials cannot be added to completed, invoiced, or cancelled work orders.");

        Guard.AgainstNegativeOrZero(quantity, nameof(quantity));
        Guard.AgainstNegative(unitPrice, nameof(unitPrice));

        _items.Add(new WorkOrderItem(description, quantity, unitPrice));
        RecalculateTotal();
        Touch();
    }

    public void Assign(Guid employeeUserId)
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Invoiced or WorkOrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot assign completed, invoiced, or cancelled work orders.");
            
        AssignedUserId = Guard.AgainstEmptyGuid(employeeUserId, nameof(employeeUserId));
        SetStatus(WorkOrderStatus.Assigned);
    }
    public void MarkAsEnRoute()
    {
        if (Status != WorkOrderStatus.Assigned)
            throw new InvalidOperationException("Only assigned work orders can be marked as en-route.");
        SetStatus(WorkOrderStatus.EnRoute);
    }

    public void ReportNoShow(string reason)
    {
        if (Status != WorkOrderStatus.EnRoute && Status != WorkOrderStatus.Assigned)
            throw new InvalidOperationException("Only en-route or assigned work orders can be reported as no-show.");
            
        CancellationReason = Guard.NotEmpty(reason, nameof(reason));
        SetStatus(WorkOrderStatus.NoShow);
    }

    public void CompleteSafetyChecklist()
    {
        IsSafetyChecklistCompleted = true;
        Touch();
    }

    public void Start(DateTime? targetCompletionDate = null)
    {
        if (!IsSafetyChecklistCompleted)
            throw new InvalidOperationException("Cannot start work order without completing the safety checklist.");
            
        if (Status != WorkOrderStatus.Assigned && Status != WorkOrderStatus.EnRoute)
            throw new InvalidOperationException("Only assigned or en-route work orders can start.");
        TargetCompletionDate = targetCompletionDate;
        SetStatus(WorkOrderStatus.InProgress);
    }

    public void CheckIn(string? notes = null)
    {
        if (Status != WorkOrderStatus.InProgress)
            throw new InvalidOperationException("You can only check-in to an in-progress work order.");
        if (_timeEntries.Any(x => !x.CheckOutTime.HasValue))
            throw new InvalidOperationException("Already checked in. Please check out first.");
            
        _timeEntries.Add(new WorkOrderTimeEntry(DateTime.UtcNow, notes));
        Touch();
    }

    public void CheckOut(string? notes = null)
    {
        if (Status != WorkOrderStatus.InProgress)
            throw new InvalidOperationException("You can only check-out of an in-progress work order.");
        
        var activeEntry = _timeEntries.LastOrDefault(x => !x.CheckOutTime.HasValue);
        if (activeEntry is null)
            throw new InvalidOperationException("No active check-in found.");
            
        activeEntry.CheckOut(DateTime.UtcNow, notes);
        Touch();
    }

    public void PutOnHold(string reason)
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Invoiced or WorkOrderStatus.Cancelled or WorkOrderStatus.NoShow)
            throw new InvalidOperationException("Cannot put closed work orders on hold.");
        
        var activeEntry = _timeEntries.LastOrDefault(x => !x.CheckOutTime.HasValue);
        if (activeEntry is not null)
        {
            activeEntry.CheckOut(DateTime.UtcNow, "Auto check-out due to placing on hold.");
        }

        HoldReason = Guard.NotEmpty(reason, nameof(reason));
        SetStatus(WorkOrderStatus.OnHold);
    }

    public void Resume()
    {
        if (Status != WorkOrderStatus.OnHold)
            throw new InvalidOperationException("Only on-hold work orders can be resumed.");
        HoldReason = null;
        SetStatus(WorkOrderStatus.InProgress);
    }

    public void Cancel(string reason)
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Invoiced)
            throw new InvalidOperationException("Completed or Invoiced work orders cannot be cancelled.");
            
        var activeEntry = _timeEntries.LastOrDefault(x => !x.CheckOutTime.HasValue);
        if (activeEntry is not null)
        {
            activeEntry.CheckOut(DateTime.UtcNow, "Auto check-out due to cancellation.");
        }

        CancellationReason = Guard.NotEmpty(reason, nameof(reason));
        SetStatus(WorkOrderStatus.Cancelled);
    }

    public void Complete(string? signature = null, string? photoUrl = null)
    {
        if (Status != WorkOrderStatus.InProgress)
            throw new InvalidOperationException("Only in-progress work orders can be completed.");
        
        if (string.IsNullOrWhiteSpace(signature) && string.IsNullOrWhiteSpace(photoUrl))
            throw new InvalidOperationException("Proof of work (signature or photo) is required to complete the work order.");

        SignatureData = signature;
        ProofOfWorkPhotoUrl = photoUrl;
        
        var activeEntry = _timeEntries.LastOrDefault(x => !x.CheckOutTime.HasValue);
        if (activeEntry is not null)
        {
            activeEntry.CheckOut(DateTime.UtcNow, "Auto check-out due to completion.");
        }
        
        SetStatus(WorkOrderStatus.Completed);
    }

    public void ApproveForBilling()
    {
        if (Status != WorkOrderStatus.Completed)
            throw new InvalidOperationException("Only completed work orders can be approved for billing.");
        SetStatus(WorkOrderStatus.ReadyForBilling);
    }


    public void Invoice()
    {
        if (Status != WorkOrderStatus.ReadyForBilling)
            throw new InvalidOperationException("Only approved (ready for billing) work orders can be invoiced.");
        SetStatus(WorkOrderStatus.Invoiced);
    }

    public void LinkSourceQuote(Guid quoteId)
    {
        Guard.AgainstEmptyGuid(quoteId, nameof(quoteId));
        if (SourceQuoteId is not null && SourceQuoteId != quoteId)
            throw new InvalidOperationException("A work order cannot be linked to another quote.");
        SourceQuoteId = quoteId;
    }

    public void LinkToProject(Guid projectId, Guid? phaseId = null)
    {
        ProjectId = Guard.AgainstEmptyGuid(projectId, nameof(projectId));
        PhaseId = phaseId;
        Touch();
    }

    public void LinkToSiteAndAsset(Guid siteId, Guid? assetId = null)
    {
        SiteId = Guard.AgainstEmptyGuid(siteId, nameof(siteId));
        AssetId = assetId;
        Touch();
    }
    
    public void LinkToParentWorkOrder(Guid parentId)
    {
        ParentWorkOrderId = Guard.AgainstEmptyGuid(parentId, nameof(parentId));
        Touch();
    }

    private void SetStatus(WorkOrderStatus nextStatus)
    {
        if (Status == WorkOrderStatus.Invoiced)
            throw new InvalidOperationException("Invoiced work orders are closed.");

        Status = nextStatus;
        Touch();
    }

    private void RecalculateTotal() => Total = _items.Sum(x => x.LineTotal);
}

public enum WorkOrderStatus
{
    Open,
    Assigned,
    EnRoute,
    InProgress,
    OnHold,
    Completed,
    ReadyForBilling,
    Invoiced,
    Cancelled,
    NoShow
}

public sealed class WorkOrderItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;

    private WorkOrderItem() { }

    public WorkOrderItem(string description, decimal quantity, decimal unitPrice)
    {
        Description = Guard.NotEmpty(description, nameof(description));
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
