using System;
using Xunit;
using FluentAssertions;
using Voltflow.Domain.Finance;

namespace Voltflow.Tests.Domain.Finance;

public class FinanceTests
{
    [Fact]
    public void Payment_Allocate_ShouldIncreaseAllocatedAmount()
    {
        var sut = new CustomerPayment(Guid.NewGuid(), 1000m, "CreditCard", DateOnly.FromDateTime(DateTime.UtcNow));
        
        sut.Allocate(400m);

        sut.AllocatedAmount.Should().Be(400m);
        sut.UnallocatedAmount.Should().Be(600m);
    }

    [Fact]
    public void Payment_Allocate_ExceedingAmount_ShouldThrowException()
    {
        var sut = new CustomerPayment(Guid.NewGuid(), 1000m, "CreditCard", DateOnly.FromDateTime(DateTime.UtcNow));
        sut.Allocate(800m);
        
        var action = () => sut.Allocate(300m);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds the unallocated amount*");
    }

    [Fact]
    public void Invoice_Allocate_ShouldIncreasePaidAmount()
    {
        var sut = new SalesInvoice(Guid.NewGuid(), "INV-001", 1000m, DateOnly.FromDateTime(DateTime.UtcNow));
        
        sut.Allocate(300m);

        sut.PaidAmount.Should().Be(300m);
        sut.RemainingAmount.Should().Be(700m);
    }

    [Fact]
    public void Invoice_Allocate_ExceedingAmount_ShouldThrowException()
    {
        var sut = new SalesInvoice(Guid.NewGuid(), "INV-001", 1000m, DateOnly.FromDateTime(DateTime.UtcNow));
        sut.Allocate(800m);
        
        var action = () => sut.Allocate(300m);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds the remaining invoice amount*");
    }

    [Fact]
    public void ProgressBilling_Approve_ShouldSetApprovedAmountAndFlag()
    {
        var sut = new ProgressBilling(Guid.NewGuid(), "PB-001", 1000m, 100m);

        sut.Approve(900m);

        sut.IsApproved.Should().BeTrue();
        sut.ApprovedAmount.Should().Be(900m);
        sut.NetPayableAmount.Should().Be(800m); // 900 - 100
    }
}
