using Voltflow.Domain.Common;

namespace Voltflow.Domain.Services;

/// <summary>
/// A Service (Hizmet) is the unified operational entity for all customer work.
/// It covers the entire lifecycle: Draft (Quote) -> Issued -> Active -> Completed/Cancelled.
/// </summary>
public sealed class Service : Entity
{
    public const int MaxItems = 200;

    public Guid CustomerId { get; private set; }

    /// <summary>Document number (e.g. SRV-20260923001).</summary>
    public string Number { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public ServiceStatus Status { get; private set; }
    public ServiceSubStatus SubStatus { get; private set; }

    // ---- quote phase properties -------------------------------------------------------------
    public DateOnly? ValidUntil { get; private set; }
    public DateTime? IssuedAt { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    
    public decimal RequiredDepositPercentage { get; private set; }
    public decimal RequiredDepositAmount => Money.Round(CurrentTotal * RequiredDepositPercentage / 100m);
    public decimal DepositPaidAmount { get; private set; }

    public bool IsChangeOrder { get; private set; }
    public Guid? ParentServiceId { get; private set; }

    // ---- active phase properties ------------------------------------------------------------
    
    /// <summary>Technician or team assigned to this service. Set via Assign command.</summary>
    public Guid? AssignedUserId { get; private set; }

    /// <summary>Sum of all partial and final invoices issued so far.</summary>
    public decimal TotalBilled { get; private set; }

    /// <summary>Current total of active items (changes when items are added/removed).</summary>
    public decimal CurrentTotal { get; private set; }

    /// <summary>BR-01: kalan_limit = current_total − total_billed.</summary>
    public decimal RemainingLimit => Money.Round(CurrentTotal - TotalBilled);

    private readonly List<ServiceItem> _items = new();
    public IReadOnlyCollection<ServiceItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<ServiceItem> ActiveItems => _items.Where(i => i.IsActive).ToList().AsReadOnly();

    // ---- optional site/asset link -----------------------------------------------------------
    public Guid? SiteId { get; private set; }
    public Guid? AssetId { get; private set; }

    private Service() { }

    /// <summary>
    /// Factory: creates a new Service draft.
    /// </summary>
    public static Service CreateDraft(
        Guid customerId, string number, string title, string? notes, DateOnly? validUntil, 
        Guid? siteId, Guid? assetId, DateTime createdAt)
    {
        var service = new Service
        {
            CustomerId = Guard.AgainstEmptyGuid(customerId, nameof(customerId)),
            Number = Guard.NotEmpty(number, nameof(number)),
            Title = Guard.NotEmpty(title, nameof(title)).Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            ValidUntil = validUntil,
            SiteId = siteId,
            AssetId = assetId,
            Status = ServiceStatus.Draft
        };
        service.SetCreatedAt(createdAt);
        return service;
    }

    public Service CopyAsDraft(string newNumber, DateTime createdAt)
    {
        var draft = CreateDraft(CustomerId, newNumber, Title, Notes, ValidUntil, SiteId, AssetId, createdAt);
        foreach (var item in ActiveItems)
        {
            draft.AddItem(item.Description, item.Quantity, item.Unit, item.UnitPrice, item.VatRate, item.Kind, createdAt, "Copied from previous revision");
        }
        return draft;
    }

    public void LinkToSiteAndAsset(Guid siteId, Guid? assetId = null)
    {
        EnsureDraft();
        SiteId = Guard.AgainstEmptyGuid(siteId, nameof(siteId));
        AssetId = assetId;
        Touch();
    }

    public void MarkAsChangeOrder(Guid parentServiceId)
    {
        EnsureDraft();
        ParentServiceId = Guard.AgainstEmptyGuid(parentServiceId, nameof(parentServiceId));
        IsChangeOrder = true;
        Touch();
    }

    // ---- lifecycle transitions --------------------------------------------------------------

    public void Issue(DateTime now)
    {
        EnsureDraft();
        if (_items.Count == 0) throw new InvalidOperationException("Cannot issue a service with no items.");
        
        Status = ServiceStatus.Issued;
        IssuedAt = now;
        Touch(now);
    }

    public void Accept(decimal? requiredDepositPercentage, DateTime now)
    {
        if (Status != ServiceStatus.Issued) throw new InvalidOperationException("Only issued services can be accepted.");
        if (IsPastValidity(DateOnly.FromDateTime(now))) throw new InvalidOperationException("The proposal has expired and can no longer be accepted.");
        
        if (requiredDepositPercentage.HasValue) 
            Guard.AgainstOutOfRange(requiredDepositPercentage.Value, 0, 100, nameof(requiredDepositPercentage));

        if (requiredDepositPercentage.HasValue) RequiredDepositPercentage = requiredDepositPercentage.Value;
        
        Status = ServiceStatus.Active;
        DecidedAt = now;
        Touch(now);
    }

    public void Reject(string reason, DateTime now)
    {
        if (Status != ServiceStatus.Issued) throw new InvalidOperationException("Only issued services can be rejected.");
        RejectionReason = Guard.NotEmpty(reason, nameof(reason)).Trim();
        Status = ServiceStatus.Rejected;
        DecidedAt = now;
        Touch(now);
    }

    public void Cancel(string? reason, DateTime now)
    {
        if (Status is ServiceStatus.Completed or ServiceStatus.Cancelled or ServiceStatus.Rejected)
            throw new InvalidOperationException($"A {Status} service cannot be cancelled.");
            
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Status = ServiceStatus.Cancelled;
        DecidedAt = now;
        Touch(now);
    }

    public void Assign(Guid userId, DateTime now)
    {
        EnsureActive();
        AssignedUserId = Guard.AgainstEmptyGuid(userId, nameof(userId));
        Touch(now);
    }

    public void Hold(DateTime now)
    {
        if (Status != ServiceStatus.Active) throw new InvalidOperationException("Only an Active service can be put on hold.");
        Status = ServiceStatus.OnHold;
        Touch(now);
    }

    public void Resume(DateTime now)
    {
        if (Status != ServiceStatus.OnHold) throw new InvalidOperationException("Only an OnHold service can be resumed.");
        Status = ServiceStatus.Active;
        Touch(now);
    }

    public void UpdateSubStatus(ServiceSubStatus subStatus, DateTime now)
    {
        SubStatus = subStatus;
        Touch(now);
    }

    // ---- item management -------------------------------------------------------------------

    public ServiceItem AddItem(
        string description, decimal quantity, string unit, decimal unitPrice, decimal vatRate, string kind,
        DateTime addedAt, string auditNote)
    {
        if (Status is ServiceStatus.Completed or ServiceStatus.Cancelled or ServiceStatus.Rejected)
            throw new InvalidOperationException($"Cannot add items to a {Status} service.");

        if (ActiveItems.Count >= MaxItems)
            throw new InvalidOperationException($"A service can have at most {MaxItems} active lines.");

        // Draft and Issued don't strictly require an audit note from the user, but we record it anyway.
        var item = ServiceItem.Create(Id, description, quantity, unit, unitPrice, vatRate, kind, addedAt, auditNote);
        _items.Add(item);
        RecalculateCurrentTotal();
        Touch(addedAt);
        return item;
    }

    public void RemoveItem(Guid itemId, DateTime removedAt, string auditNote)
    {
        if (Status is ServiceStatus.Completed or ServiceStatus.Cancelled or ServiceStatus.Rejected)
            throw new InvalidOperationException($"Cannot remove items from a {Status} service.");

        var item = _items.FirstOrDefault(i => i.Id == itemId && i.IsActive)
            ?? throw new InvalidOperationException("Item not found or already removed.");
            
        item.Remove(removedAt, auditNote);
        RecalculateCurrentTotal();
        Touch(removedAt);
    }

    public void UpdateDraftDetails(string title, string? notes, DateOnly? validUntil, Guid? siteId, Guid? assetId)
    {
        EnsureDraft();
        Title = Guard.NotEmpty(title, nameof(title)).Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ValidUntil = validUntil;
        SiteId = siteId;
        AssetId = assetId;
        Touch();
    }

    // ---- financial -------------------------------------------------------------------------

    public void PayDeposit(decimal amount, DateTime now)
    {
        if (Status is ServiceStatus.Completed or ServiceStatus.Cancelled or ServiceStatus.Rejected) 
            throw new InvalidOperationException($"Cannot pay deposit on a {Status} service.");
            
        Guard.AgainstNegativeOrZero(amount, nameof(amount));
        var paid = Money.Round(amount);
        if (paid <= 0) throw new InvalidOperationException("The deposit must be at least one cent.");
        if (DepositPaidAmount + paid > CurrentTotal) throw new InvalidOperationException("The deposits cannot exceed the service total.");

        DepositPaidAmount += paid;
        Touch(now);
    }

    public void RecordPartialInvoice(decimal amount, DateTime now)
    {
        EnsureActive();
        if (amount <= 0) throw new InvalidOperationException("Invoice amount must be positive.");

        if (Money.Round(RemainingLimit - amount) <= 0)
            throw new InvalidOperationException(
                "This amount would reduce the remaining limit to zero. " +
                "Use CompleteService to issue the final invoice and close the service.");

        if (amount > RemainingLimit)
            throw new InvalidOperationException($"Invoice amount ({amount:N2}) exceeds the remaining limit ({RemainingLimit:N2}).");

        TotalBilled = Money.Round(TotalBilled + amount);
        Touch(now);
    }

    public void CompleteService(decimal? finalAmountOverride, DateTime now)
    {
        EnsureActive();
        var finalAmount = finalAmountOverride.HasValue ? finalAmountOverride.Value : RemainingLimit;
        if (finalAmount < 0) throw new InvalidOperationException("Final invoice amount cannot be negative.");

        TotalBilled = Money.Round(TotalBilled + finalAmount);
        Status = ServiceStatus.Completed;
        Touch(now);
    }

    // ---- private ---------------------------------------------------------------------------

    private void EnsureDraft()
    {
        if (Status != ServiceStatus.Draft) throw new InvalidOperationException($"Operation not allowed on a {Status} service. Must be Draft.");
    }

    private void EnsureActive()
    {
        if (Status is not (ServiceStatus.Active or ServiceStatus.OnHold))
            throw new InvalidOperationException($"Operation not allowed on a {Status} service. Must be Active or OnHold.");
    }

    private void RecalculateCurrentTotal()
        => CurrentTotal = Money.Round(ActiveItems.Sum(i => i.LineTotal));
        
    public bool IsPastValidity(DateOnly today) => Status == ServiceStatus.Issued && ValidUntil is { } until && today > until;
}
