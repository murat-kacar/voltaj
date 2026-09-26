using Voltflow.Domain.Common;

namespace Voltflow.Domain.Services;

/// <summary>
/// A single line of work or material within a Service.
/// Items may be added or removed during the Active phase; every change is
/// recorded with a timestamp and an audit note (D1 audit trail requirement).
/// </summary>
public sealed class ServiceItem : Entity
{
    public const decimal DefaultVatRate = 20m;
    public const decimal MaxQuantity = 100_000m;
    public const decimal MaxUnitPrice = 10_000_000m;

    public Guid ServiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal VatRate { get; private set; }
    public string Kind { get; private set; } = string.Empty;  // e.g. "Material", "Labour"

    /// <summary>UTC moment the item was added to the service (after acceptance).</summary>
    public DateTime AddedAt { get; private set; }

    /// <summary>UTC moment the item was removed, or null if still active.</summary>
    public DateTime? RemovedAt { get; private set; }

    /// <summary>Mandatory note recorded when the item is added or removed (H9 audit).</summary>
    public string AuditNote { get; private set; } = string.Empty;

    public bool IsActive => RemovedAt is null;

    public decimal LineTotal => Money.Round(Quantity * UnitPrice);
    public decimal VatAmount => Money.Round(LineTotal * (VatRate / 100m));

    private ServiceItem() { }

    internal static ServiceItem Create(
        Guid serviceId,
        string description,
        decimal quantity,
        string unit,
        decimal unitPrice,
        decimal vatRate,
        string kind,
        DateTime addedAt,
        string auditNote)
    {
        return new ServiceItem
        {
            ServiceId = serviceId,
            Description = Guard.NotEmpty(description, nameof(description)).Trim(),
            Quantity = quantity > 0 ? quantity : throw new InvalidOperationException("Quantity must be positive."),
            Unit = Guard.NotEmpty(unit, nameof(unit)).Trim(),
            UnitPrice = unitPrice >= 0 ? unitPrice : throw new InvalidOperationException("UnitPrice must be non-negative."),
            VatRate = vatRate >= 0 ? vatRate : throw new InvalidOperationException("VatRate must be non-negative."),
            Kind = Guard.NotEmpty(kind, nameof(kind)).Trim(),
            AddedAt = addedAt,
            AuditNote = Guard.NotEmpty(auditNote, nameof(auditNote)).Trim(),
        };
    }

    internal void Remove(DateTime removedAt, string auditNote)
    {
        if (RemovedAt is not null) throw new InvalidOperationException("Item is already removed.");
        RemovedAt = removedAt;
        AuditNote = Guard.NotEmpty(auditNote, nameof(auditNote)).Trim();
        Touch(removedAt);
    }
}
