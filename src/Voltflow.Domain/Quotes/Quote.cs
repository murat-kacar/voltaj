using Voltflow.Domain.Common;

namespace Voltflow.Domain.Quotes;

/// <summary>
/// A priced proposal to a customer. Prices are what the customer pays, VAT included; the VAT of each line is worked out from its
/// rate for the document. Only a draft can be changed: once issued, the items are frozen and a revision is a copy.
/// Every method checks before it changes anything, so a refused call leaves the quote as it was.
/// </summary>
public sealed class Quote : Entity
{
    /// <summary>How long a quote stays valid when nobody chose a date: about two weeks.</summary>
    public const int DefaultValidityDays = 15;

    /// <summary>More lines than this is a catalogue, not a quote; it also keeps the total inside its column.</summary>
    public const int MaxItems = 200;

    public Guid CustomerId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    /// <summary>What the customer pays, VAT included.</summary>
    public decimal Total { get; private set; }

    public QuoteState State { get; private set; }
    public string? RejectionReason { get; private set; }
    public decimal RequiredDepositPercentage { get; private set; }
    public decimal DepositPaidAmount { get; private set; }
    public decimal RequiredDepositAmount => Money.Round(Total * (RequiredDepositPercentage / 100m));

    /// <summary>The VAT that is already inside <see cref="Total"/>.</summary>
    public decimal VatTotal => _items.Sum(item => item.VatAmount);

    public DateTime? IssuedAt { get; private set; }

    /// <summary>The last day the customer can accept. After it the quote expires.</summary>
    public DateOnly? ValidUntil { get; private set; }

    /// <summary>When the quote was accepted, rejected or expired.</summary>
    public DateTime? DecidedAt { get; private set; }

    public Guid? SiteId { get; private set; }
    public Guid? AssetId { get; private set; }

    public bool IsChangeOrder { get; private set; }
    public Guid? ParentWorkOrderId { get; private set; }

    private readonly List<QuoteItem> _items = new();
    public IReadOnlyCollection<QuoteItem> Items => _items.AsReadOnly();

    private Quote() { }

    /// <summary>For tests and tools that do not number documents: the number is the moment of creation.</summary>
    public Quote(Guid customerId, string title) : this($"Q-{DateTime.UtcNow:yyyyMMddHHmmss}", customerId, title)
    {
    }

    public Quote(string number, Guid customerId, string title)
    {
        var cleanNumber = Guard.NotEmpty(number, nameof(number)).Trim();
        var cleanTitle = Guard.NotEmpty(title, nameof(title)).Trim();
        CustomerId = Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Number = cleanNumber;
        Title = cleanTitle;
        State = QuoteState.Draft;
    }

    /// <summary>
    /// A new draft. Everything is checked before the number is taken, so a refused draft never uses one up; the number comes last,
    /// from <paramref name="nextNumber"/>.
    /// </summary>
    public static Quote Create(
        Func<string> nextNumber, Guid customerId, string title, string? notes, DateOnly? validUntil, Guid? siteId, Guid? assetId, IEnumerable<QuoteItemDraft> items)
    {
        var quote = new Quote("pending", customerId, title);
        quote.Update(title, notes, validUntil, siteId, assetId, items);
        quote.Number = Guard.NotEmpty(nextNumber(), nameof(nextNumber)).Trim();
        return quote;
    }

    // ---- the draft ---------------------------------------------------------------------------------------

    /// <summary>Changes the draft: its details and all of its lines at once. Everything is checked before anything changes.</summary>
    public void Update(string title, string? notes, DateOnly? validUntil, Guid? siteId, Guid? assetId, IEnumerable<QuoteItemDraft> items)
    {
        EnsureDraft();
        var cleanTitle = Guard.NotEmpty(title, nameof(title)).Trim();
        if (siteId == Guid.Empty || assetId == Guid.Empty) throw new ArgumentException("An address or device id cannot be empty.");
        if (assetId is not null && siteId is null) throw new InvalidOperationException("A device belongs to an address: choose the address too.");

        var lines = new List<QuoteItem>();
        foreach (var draft in items)
        {
            if (lines.Count >= MaxItems) throw new InvalidOperationException($"A quote can have at most {MaxItems} lines.");
            lines.Add(new QuoteItem(lines.Count + 1, draft.Kind, draft.Description, draft.Unit, draft.Quantity, draft.UnitPrice, draft.VatRate));
        }

        Title = cleanTitle;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ValidUntil = validUntil;
        SiteId = siteId;
        AssetId = assetId;
        _items.Clear();
        _items.AddRange(lines);
        RecalculateTotal();
        Touch();
    }

    public QuoteItem AddItem(
        string description, decimal quantity, decimal unitPrice, string? unit = null, decimal vatRate = QuoteItem.DefaultVatRate, QuoteItemKind kind = QuoteItemKind.Service)
    {
        EnsureDraft();
        if (_items.Count >= MaxItems) throw new InvalidOperationException($"A quote can have at most {MaxItems} lines.");

        var item = new QuoteItem(_items.Count + 1, kind, description, unit, quantity, unitPrice, vatRate);
        _items.Add(item);
        RecalculateTotal();
        Touch();
        return item;
    }

    /// <summary>A new draft with the same customer, address, notes and items: how a quote is revised once it is no longer a draft.</summary>
    public Quote CopyAsDraft(Func<string> nextNumber)
    {
        var lines = _items
            .OrderBy(item => item.LineNumber)
            .Select(item => new QuoteItemDraft(item.Kind, item.Description, item.Quantity, item.UnitPrice, item.Unit, item.VatRate));
        return Create(nextNumber, CustomerId, Title, Notes, validUntil: null, SiteId, AssetId, lines);
    }

    // ---- the decision ------------------------------------------------------------------------------------

    public void Issue(DateTime? now = null)
    {
        if (State != QuoteState.Draft)
            throw new InvalidOperationException("Only draft quotes can be issued.");
        if (_items.Count == 0)
            throw new InvalidOperationException("A quote must contain at least one item before it can be issued.");

        var issuedAt = now ?? DateTime.UtcNow;
        var today = DateOnly.FromDateTime(issuedAt);
        var validUntil = ValidUntil ?? today.AddDays(DefaultValidityDays);
        if (validUntil < today)
            throw new InvalidOperationException("The validity date cannot be before the day the quote is issued.");

        IssuedAt = issuedAt;
        ValidUntil = validUntil;
        State = QuoteState.Issued;
        Touch();
    }

    public void Accept(decimal? requiredDepositPercentage = null, DateTime? now = null)
    {
        if (State != QuoteState.Issued) throw new InvalidOperationException("Only issued quotes can be accepted.");
        var decidedAt = now ?? DateTime.UtcNow;
        if (IsPastValidity(DateOnly.FromDateTime(decidedAt)))
            throw new InvalidOperationException("The quote has expired and can no longer be accepted.");
        if (requiredDepositPercentage.HasValue) Guard.AgainstOutOfRange(requiredDepositPercentage.Value, 0, 100, nameof(requiredDepositPercentage));

        if (requiredDepositPercentage.HasValue) RequiredDepositPercentage = requiredDepositPercentage.Value;
        State = QuoteState.Accepted;
        DecidedAt = decidedAt;
        Touch();
    }

    public void PayDeposit(decimal amount)
    {
        if (State != QuoteState.Accepted) throw new InvalidOperationException("Only accepted quotes can receive deposits.");
        Guard.AgainstNegativeOrZero(amount, nameof(amount));
        var paid = Money.Round(amount);
        if (paid <= 0) throw new InvalidOperationException("The deposit must be at least one cent.");
        if (DepositPaidAmount + paid > Total) throw new InvalidOperationException("The deposits cannot add up to more than the quote total.");

        DepositPaidAmount += paid;
        Touch();
    }

    public void Reject(string rejectionReason, DateTime? now = null)
    {
        if (State != QuoteState.Issued) throw new InvalidOperationException("Only issued quotes can be rejected.");
        var reason = Guard.NotEmpty(rejectionReason, nameof(rejectionReason)).Trim();

        RejectionReason = reason;
        State = QuoteState.Rejected;
        DecidedAt = now ?? DateTime.UtcNow;
        Touch();
    }

    public void Expire(DateTime? now = null)
    {
        if (State is QuoteState.Accepted or QuoteState.Rejected)
            throw new InvalidOperationException("Terminal quotes cannot expire.");
        if (State == QuoteState.Expired) return;

        State = QuoteState.Expired;
        DecidedAt = now ?? DateTime.UtcNow;
        Touch();
    }

    /// <summary>Whether an issued quote has outlived its validity date.</summary>
    public bool IsPastValidity(DateOnly today) => State == QuoteState.Issued && ValidUntil is { } until && today > until;

    /// <summary>Expires the quote when it has outlived its validity date. Returns whether it did.</summary>
    public bool ExpireIfDue(DateTime now)
    {
        if (!IsPastValidity(DateOnly.FromDateTime(now))) return false;
        Expire(now);
        return true;
    }

    // ---- links -------------------------------------------------------------------------------------------

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

    private void EnsureDraft()
    {
        if (State != QuoteState.Draft) throw new InvalidOperationException("Only a draft quote can be changed.");
    }

    private void RecalculateTotal() => Total = _items.Sum(item => item.LineTotal);
}

public enum QuoteState
{
    Draft,
    Issued,
    Accepted,
    Rejected,
    Expired
}

/// <summary>What a line is for; it groups the lines of the document.</summary>
public enum QuoteItemKind
{
    Material,
    Labor,
    Service
}

/// <summary>A line as it is asked for; it becomes a <see cref="QuoteItem"/> once the quote checks it.</summary>
public sealed record QuoteItemDraft(
    QuoteItemKind Kind, string Description, decimal Quantity, decimal UnitPrice, string? Unit = null, decimal VatRate = QuoteItem.DefaultVatRate);

public sealed class QuoteItem
{
    public const decimal DefaultVatRate = 20m;

    /// <summary>Bounds that keep a line, and a whole quote of them, inside the money columns.</summary>
    public const decimal MaxQuantity = 100_000m;
    public const decimal MaxUnitPrice = 10_000_000m;

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>The position on the document, starting at 1.</summary>
    public int LineNumber { get; private set; }

    public QuoteItemKind Kind { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Unit { get; private set; } = "adet";
    public decimal Quantity { get; private set; }

    /// <summary>What the customer pays per unit, VAT included.</summary>
    public decimal UnitPrice { get; private set; }

    public decimal VatRate { get; private set; } = DefaultVatRate;
    public decimal LineTotal => Money.Round(Quantity * UnitPrice);

    /// <summary>The VAT that is already inside <see cref="LineTotal"/>.</summary>
    public decimal VatAmount => Money.Round(LineTotal * VatRate / (100m + VatRate));

    private QuoteItem() { }

    internal QuoteItem(int lineNumber, QuoteItemKind kind, string description, string? unit, decimal quantity, decimal unitPrice, decimal vatRate)
    {
        var cleanDescription = Guard.NotEmpty(description, nameof(description)).Trim();
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind), "Unknown line kind.");
        Guard.AgainstNegativeOrZero(quantity, nameof(quantity));
        if (quantity > MaxQuantity || Math.Round(quantity, 2) != quantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), $"The quantity can be at most {MaxQuantity:0}, with at most two decimals.");
        Guard.AgainstNegative(unitPrice, nameof(unitPrice));
        if (unitPrice > MaxUnitPrice) throw new ArgumentOutOfRangeException(nameof(unitPrice), $"The unit price can be at most {MaxUnitPrice:0}.");
        Guard.AgainstOutOfRange(vatRate, 0, 100, nameof(vatRate));

        LineNumber = lineNumber;
        Kind = kind;
        Description = cleanDescription;
        Unit = string.IsNullOrWhiteSpace(unit) ? "adet" : unit.Trim();
        Quantity = quantity;
        UnitPrice = Money.Round(unitPrice);
        VatRate = vatRate;
    }
}
