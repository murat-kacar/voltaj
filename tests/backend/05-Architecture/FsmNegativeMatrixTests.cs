using System.Reflection;
using FluentAssertions;
using Voltflow.Domain.WorkOrders;
using Voltflow.Domain.Quotes;
using Xunit;

namespace Voltflow.Tests;

[Trait("VUT", "03504")]
public class FsmNegativeMatrixTests
{
    private WorkOrder CreateWorkOrderInState(WorkOrderStatus status)
    {
        var order = new WorkOrder(Guid.NewGuid(), "FSM Negative Test");
        typeof(WorkOrder).GetProperty(nameof(WorkOrder.Status))?.SetValue(order, status);
        return order;
    }

    [Theory]
    [Trait("VUT", "03504")]
    [InlineData(WorkOrderStatus.Open)]
    [InlineData(WorkOrderStatus.Assigned)]
    [InlineData(WorkOrderStatus.EnRoute)]
    [InlineData(WorkOrderStatus.InProgress)]
    [InlineData(WorkOrderStatus.OnHold)]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.NoShow)]
    public void Invoice_WhenNotReadyForBilling_ShouldThrowInvalidOperationException(WorkOrderStatus status)
    {
        var order = CreateWorkOrderInState(status);
        var act = () => order.Invoice();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only approved (ready for billing) work orders can be invoiced.");
    }

    [Theory]
    [Trait("VUT", "03402")]
    [InlineData(WorkOrderStatus.Open)]
    [InlineData(WorkOrderStatus.Assigned)]
    [InlineData(WorkOrderStatus.EnRoute)]
    [InlineData(WorkOrderStatus.OnHold)]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.ReadyForBilling)]
    [InlineData(WorkOrderStatus.Invoiced)]
    [InlineData(WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.NoShow)]
    public void Complete_WhenNotInProgress_ShouldThrowInvalidOperationException(WorkOrderStatus status)
    {
        var order = CreateWorkOrderInState(status);
        var act = () => order.Complete("Signature", "Photo");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only in-progress work orders can be completed.");
    }

    [Theory]
    [InlineData(WorkOrderStatus.Open)]
    [InlineData(WorkOrderStatus.Assigned)]
    [InlineData(WorkOrderStatus.EnRoute)]
    [InlineData(WorkOrderStatus.OnHold)]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.ReadyForBilling)]
    [InlineData(WorkOrderStatus.Invoiced)]
    [InlineData(WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.NoShow)]
    public void CheckIn_WhenNotInProgress_ShouldThrowInvalidOperationException(WorkOrderStatus status)
    {
        var order = CreateWorkOrderInState(status);
        var act = () => order.CheckIn();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("You can only check-in to an in-progress work order.");
    }

    [Theory]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Invoiced)]
    [InlineData(WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.NoShow)]
    public void PutOnHold_WhenClosed_ShouldThrowInvalidOperationException(WorkOrderStatus status)
    {
        var order = CreateWorkOrderInState(status);
        var act = () => order.PutOnHold("Missing parts");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot put closed work orders on hold.");
    }

    [Theory]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Invoiced)]
    public void Cancel_WhenCompletedOrInvoiced_ShouldThrowInvalidOperationException(WorkOrderStatus status)
    {
        var order = CreateWorkOrderInState(status);
        var act = () => order.Cancel("Customer requested");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Completed or Invoiced work orders cannot be cancelled.");
    }

    [Theory]
    [InlineData(WorkOrderStatus.Open)]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.ReadyForBilling)]
    [InlineData(WorkOrderStatus.Invoiced)]
    [InlineData(WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.NoShow)]
    public void Start_WhenNotAssignedOrEnRoute_ShouldThrowInvalidOperationException(WorkOrderStatus status)
    {
        var order = CreateWorkOrderInState(status);
        order.CompleteSafetyChecklist(); // bypass safety check
        var act = () => order.Start();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only assigned or en-route work orders can start.");
    }

    [Fact]
    public void Start_WithoutSafetyChecklist_ShouldThrowInvalidOperationException()
    {
        var order = CreateWorkOrderInState(WorkOrderStatus.Assigned);
        var act = () => order.Start();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot start work order without completing the safety checklist.");
    }
}
