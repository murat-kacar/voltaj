using System;
using FluentAssertions;
using Voltflow.Domain.Inventory;
using Xunit;

namespace Voltflow.Tests.Domain.Inventory;

public class ProductTests
{
    [Fact]
    public void Create_TrimsTextAndDefaultsTheUnit()
    {
        var product = new Product("  SW-100 ", "  Wall switch ", "  8690000000017 ", null, 12.345m, 20m, true);

        product.Code.Should().Be("SW-100");
        product.Name.Should().Be("Wall switch");
        product.Barcode.Should().Be("8690000000017");
        product.Unit.Should().Be("adet");
        product.SalePrice.Should().Be(12.35m);
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_TreatsABlankBarcodeAsNone()
    {
        new Product("SW-100", "Wall switch", "   ", "adet", 10m, 20m, true).Barcode.Should().BeNull();
    }

    [Fact]
    public void Create_RejectsANegativePrice_AVatAboveOneHundred_AndABlankName()
    {
        var price = () => new Product("A", "Item", null, null, -0.01m, 20m, true);
        var vat = () => new Product("A", "Item", null, null, 1m, 100.5m, true);
        var name = () => new Product("A", " ", null, null, 1m, 20m, true);

        price.Should().Throw<ArgumentException>();
        vat.Should().Throw<ArgumentException>();
        name.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesEverythingButTheCode_AndCanDeactivate()
    {
        var product = new Product("SW-100", "Wall switch", null, "adet", 10m, 20m, true);

        product.Update("Cable 2m", "8690000000024", "metre", 55m, 10m, false, isActive: false);

        product.Code.Should().Be("SW-100");
        product.Name.Should().Be("Cable 2m");
        product.Barcode.Should().Be("8690000000024");
        product.Unit.Should().Be("metre");
        product.SalePrice.Should().Be(55m);
        product.VatRate.Should().Be(10m);
        product.TracksStock.Should().BeFalse();
        product.IsActive.Should().BeFalse();
    }
}
