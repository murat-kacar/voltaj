using System;
using Xunit;
using FluentAssertions;
using Voltflow.Domain.Inventory;

namespace Voltflow.Tests.Domain.Inventory;

public class InventoryTests
{
    [Fact]
    public void MaterialStock_Adjust_ShouldUpdateQuantity()
    {
        var sut = new MaterialStock(Guid.NewGuid(), "MAT-01", "Wire", 100m);
        
        sut.Adjust(50m);
        sut.QuantityOnHand.Should().Be(150m);

        sut.Adjust(-20m);
        sut.QuantityOnHand.Should().Be(130m);
    }

    [Fact]
    public void MaterialStock_Adjust_BelowZero_ShouldThrowException()
    {
        var sut = new MaterialStock(Guid.NewGuid(), "MAT-01", "Wire", 10m);
        
        var action = () => sut.Adjust(-15m);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot go below zero*");
    }

    [Fact]
    public void MaterialStock_Reserve_ShouldIncreaseReservedQuantity()
    {
        var sut = new MaterialStock(Guid.NewGuid(), "MAT-01", "Wire", 100m);
        
        sut.Reserve(30m);

        sut.ReservedQuantity.Should().Be(30m);
        sut.AvailableQuantity.Should().Be(70m);
    }

    [Fact]
    public void MaterialStock_Reserve_ExceedingAvailable_ShouldThrowException()
    {
        var sut = new MaterialStock(Guid.NewGuid(), "MAT-01", "Wire", 100m);
        
        var action = () => sut.Reserve(110m);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient available quantity*");
    }

    [Fact]
    public void MaterialStock_Release_ShouldDecreaseReservedQuantity()
    {
        var sut = new MaterialStock(Guid.NewGuid(), "MAT-01", "Wire", 100m);
        sut.Reserve(40m);
        
        sut.Release(15m);

        sut.ReservedQuantity.Should().Be(25m);
        sut.AvailableQuantity.Should().Be(75m);
    }

    [Fact]
    public void StockMovement_Constructor_ShouldInitializeProperties()
    {
        var sut = new StockMovement("MAT-01", 10m, StockMovementType.In);
        
        sut.MaterialCode.Should().Be("MAT-01");
        sut.QuantityDelta.Should().Be(10m);
        sut.Direction.Should().Be(StockMovementType.In);
    }
}
