using System;
using Xunit;
using FluentAssertions;
using Voltflow.Domain.WorkOrders;

namespace Voltflow.Tests.Domain.WorkOrders;

public class WorkOrderTests
{
    [Fact]
    public void Constructor_ShouldCreateOpenWorkOrder()
    {
        var customerId = Guid.NewGuid();
        var sut = new WorkOrder(customerId, "Test Work");

        sut.CustomerId.Should().Be(customerId);
        sut.Title.Should().Be("Test Work");
        sut.Status.Should().Be(WorkOrderStatus.Open);
    }

    [Fact]
    public void Assign_ShouldTransitionToAssigned()
    {
        var sut = new WorkOrder(Guid.NewGuid(), "Test Work");
        var techId = Guid.NewGuid();

        sut.Assign(techId);

        sut.Status.Should().Be(WorkOrderStatus.Assigned);
        sut.AssignedUserId.Should().Be(techId);
    }

    [Fact]
    public void Start_ShouldTransitionToInProgress_WhenSafetyChecklistCompleted()
    {
        var sut = new WorkOrder(Guid.NewGuid(), "Test Work");
        sut.Assign(Guid.NewGuid());
        sut.CompleteSafetyChecklist();
        
        sut.Start();

        sut.Status.Should().Be(WorkOrderStatus.InProgress);
    }

    [Fact]
    public void Complete_ShouldTransitionToCompleted()
    {
        var sut = new WorkOrder(Guid.NewGuid(), "Test Work");
        sut.Assign(Guid.NewGuid());
        sut.CompleteSafetyChecklist();
        sut.Start();
        
        sut.Complete("Signature");

        sut.Status.Should().Be(WorkOrderStatus.Completed);
    }

    [Fact]
    public void InvalidTransition_ShouldThrowException()
    {
        var sut = new WorkOrder(Guid.NewGuid(), "Test Work");
        
        // Cannot complete an Open work order
        var action = () => sut.Complete("Signature");
        action.Should().Throw<InvalidOperationException>();
    }
}
