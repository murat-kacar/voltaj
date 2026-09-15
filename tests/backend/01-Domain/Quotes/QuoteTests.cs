using System;
using Xunit;
using FluentAssertions;
using Voltflow.Domain.Quotes;

namespace Voltflow.Tests.Domain.Quotes;

public class QuoteTests
{
    [Fact]
    public void Constructor_ShouldCreateDraftQuote()
    {
        var customerId = Guid.NewGuid();
        var sut = new Quote(customerId, "Test Quote");

        sut.CustomerId.Should().Be(customerId);
        sut.Title.Should().Be("Test Quote");
        sut.State.Should().Be(QuoteState.Draft);
        sut.Number.Should().StartWith("Q-");
    }

    [Fact]
    public void AddItem_ShouldRecalculateTotal()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");
        
        sut.AddItem("Item 1", 2, 100);
        sut.AddItem("Item 2", 1, 50);

        sut.Total.Should().Be(250);
        sut.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Issue_WithoutItems_ShouldThrowException()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");

        var action = () => sut.Issue();
        
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public void Issue_WithItems_ShouldTransitionToIssued()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");
        sut.AddItem("Item 1", 1, 100);

        sut.Issue();

        sut.State.Should().Be(QuoteState.Issued);
    }

    [Fact]
    public void Accept_WhenIssued_ShouldTransitionToAccepted()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");
        sut.AddItem("Item 1", 1, 100);
        sut.Issue();

        sut.Accept(20); // 20% deposit

        sut.State.Should().Be(QuoteState.Accepted);
        sut.RequiredDepositPercentage.Should().Be(20);
        sut.RequiredDepositAmount.Should().Be(20); // 20% of 100
    }

    [Fact]
    public void PayDeposit_ShouldIncreaseDepositPaidAmount()
    {
        var sut = new Quote(Guid.NewGuid(), "Test Quote");
        sut.AddItem("Item 1", 1, 100);
        sut.Issue();
        sut.Accept(20);

        sut.PayDeposit(20);

        sut.DepositPaidAmount.Should().Be(20);
    }
}
