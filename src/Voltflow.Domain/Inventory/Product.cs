using Voltflow.Domain.Common;

namespace Voltflow.Domain.Inventory;

/// <summary>The price-list side of a material: what it is called at the counter, its barcode, its price and its VAT.
/// <see cref="Code"/> is the material code that a stock row is keyed by.</summary>
public sealed class Product : Entity
{
    public const string DefaultUnit = "adet";

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public string Unit { get; private set; } = DefaultUnit;

    /// <summary>What the customer pays per unit, VAT included.</summary>
    public decimal SalePrice { get; private set; }

    /// <summary>The VAT percentage contained in <see cref="SalePrice"/>.</summary>
    public decimal VatRate { get; private set; }

    /// <summary>False for services and other items that have no stock to decrement.</summary>
    public bool TracksStock { get; private set; } = true;

    public bool IsActive { get; private set; } = true;

    private Product() { }

    public Product(string code, string name, string? barcode, string? unit, decimal salePrice, decimal vatRate, bool tracksStock)
    {
        Code = Guard.NotEmpty(code, nameof(code)).Trim();
        Apply(name, barcode, unit, salePrice, vatRate, tracksStock);
    }

    public void Update(string name, string? barcode, string? unit, decimal salePrice, decimal vatRate, bool tracksStock, bool isActive)
    {
        Apply(name, barcode, unit, salePrice, vatRate, tracksStock);
        IsActive = isActive;
        Touch();
    }

    private void Apply(string name, string? barcode, string? unit, decimal salePrice, decimal vatRate, bool tracksStock)
    {
        Name = Guard.NotEmpty(name, nameof(name)).Trim();
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        Unit = string.IsNullOrWhiteSpace(unit) ? DefaultUnit : unit.Trim();
        SalePrice = Money.Round(Guard.AgainstNegative(salePrice, nameof(salePrice)));
        VatRate = Guard.AgainstOutOfRange(vatRate, 0, 100, nameof(vatRate));
        TracksStock = tracksStock;
    }
}
