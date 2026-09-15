using Voltflow.Domain.Common;

namespace Voltflow.Domain.Quotes;

public sealed class Quote : Entity
{
    public Guid CustomerId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public decimal Total { get; private set; }
    public QuoteState State { get; private set; }
    public string? RejectionReason { get; private set; }
    public decimal RequiredDepositPercentage { get; private set; }
    public decimal DepositPaidAmount { get; private set; }
    public decimal RequiredDepositAmount => Total * (RequiredDepositPercentage / 100m);
    
    public Guid? SiteId { get; private set; }
    public Guid? AssetId { get; private set; }
    
    public bool IsChangeOrder { get; private set; }
    public Guid? ParentWorkOrderId { get; private set; }

    private readonly List<QuoteItem> _items = new();
    public IReadOnlyCollection<QuoteItem> Items => _items.AsReadOnly();

    private Quote() { }

    public Quote(Guid customerId, string title)
    {
        CustomerId = Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Title = Guard.NotEmpty(title, nameof(title));
        Number = $"Q-{DateTime.UtcNow:yyyyMMddHHmmss}";
        State = QuoteState.Draft;
    }

    public void AddItem(string description, decimal quantity, decimal unitPrice)
    {
        Guard.AgainstNegativeOrZero(quantity, nameof(quantity));
        Guard.AgainstNegative(unitPrice, nameof(unitPrice));

        _items.Add(new QuoteItem(description, quantity, unitPrice));
        RecalculateTotal();
        Touch();
    }

    public void Issue()
    {
        if (State != QuoteState.Draft)
            throw new InvalidOperationException("Only draft quotes can be issued.");
        if (_items.Count == 0)
            throw new InvalidOperationException("A quote must contain at least one item before it can be issued.");

        State = QuoteState.Issued;
        Touch();
    }

    public void Accept(decimal? requiredDepositPercentage = null) 
    { 
        if (State != QuoteState.Issued) throw new InvalidOperationException("Only issued quotes can be accepted."); 
        if (requiredDepositPercentage.HasValue)
        {
            Guard.AgainstOutOfRange(requiredDepositPercentage.Value, 0, 100, nameof(requiredDepositPercentage));
            RequiredDepositPercentage = requiredDepositPercentage.Value;
        }
        State = QuoteState.Accepted; 
        Touch(); 
    }

    public void PayDeposit(decimal amount)
    {
        if (State != QuoteState.Accepted) throw new InvalidOperationException("Only accepted quotes can receive deposits.");
        Guard.AgainstNegativeOrZero(amount, nameof(amount));
        DepositPaidAmount += amount;
        Touch();
    }
    public void Reject(string rejectionReason)
    {
        if (State != QuoteState.Issued) throw new InvalidOperationException("Only issued quotes can be rejected.");
        RejectionReason = Guard.NotEmpty(rejectionReason, nameof(rejectionReason)).Trim();
        State = QuoteState.Rejected;
        Touch();
    }
    public void Expire()
    {
        if (State is QuoteState.Accepted or QuoteState.Rejected)
            throw new InvalidOperationException("Terminal quotes cannot expire.");
        State = QuoteState.Expired;
        Touch();
    }

    public void LinkToSiteAndAsset(Guid siteId, Guid? assetId = null)
    {
        SiteId = Guard.AgainstEmptyGuid(siteId, nameof(siteId));
        AssetId = assetId;
        Touch();
    }
    
    public void MarkAsChangeOrder(Guid parentWorkOrderId)
    {
        ParentWorkOrderId = Guard.AgainstEmptyGuid(parentWorkOrderId, nameof(parentWorkOrderId));
        IsChangeOrder = true;
        Touch();
    }

    private void RecalculateTotal() => Total = _items.Sum(x => x.LineTotal);
}

public enum QuoteState
{
    Draft,
    Issued,
    Accepted,
    Rejected,
    Expired
}

public sealed class QuoteItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;

    private QuoteItem() { }

    public QuoteItem(string description, decimal quantity, decimal unitPrice)
    {
        Description = Guard.NotEmpty(description, nameof(description));
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
