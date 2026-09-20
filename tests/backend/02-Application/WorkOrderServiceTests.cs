using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Voltflow.Application.Services;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.WorkOrders;

namespace Voltflow.Tests.Application;

public class WorkOrderServiceTests
{
    private readonly Mock<IWorkOrderRepository> _repositoryMock;
    private readonly Mock<IOutboxRepository> _outboxMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ICommandJournal> _commandJournalMock;
    private readonly Mock<IOperationContext> _operationContextMock;
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Guid _operationId = Guid.NewGuid();
    private readonly WorkOrderService _sut;

    public WorkOrderServiceTests()
    {
        _repositoryMock = new Mock<IWorkOrderRepository>();
        _outboxMock = new Mock<IOutboxRepository>();
        _currentUserMock = new Mock<ICurrentUser>();
        _commandJournalMock = new Mock<ICommandJournal>();
        _operationContextMock = new Mock<IOperationContext>();
        _paymentRepositoryMock = new Mock<IPaymentRepository>();

        _currentUserMock.Setup(x => x.Roles).Returns(new[] { "Admin" });
        _operationContextMock.Setup(x => x.OperationId).Returns(_operationId);

        _sut = new WorkOrderService(_repositoryMock.Object, _outboxMock.Object, _currentUserMock.Object,
            _commandJournalMock.Object, _operationContextMock.Object, _paymentRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenValidRequest()
    {
        var request = new CreateWorkOrderRequest(Guid.NewGuid(), "Test Title");
        
        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Title");
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCustomerIdIsEmpty()
    {
        var request = new CreateWorkOrderRequest(Guid.Empty, "Test Title");
        
        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("CustomerId is required");
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnWorkOrder_WhenAdmin()
    {
        var order = new WorkOrder(Guid.NewGuid(), "Test Order");
        _repositoryMock.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await _sut.GetByIdAsync(order.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task AssignAsync_ShouldResolveCommandBeforeSaving_WhenSuccessful()
    {
        var order = new WorkOrder(Guid.NewGuid(), "Test Order");
        _repositoryMock.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var sequence = new MockSequence();
        _commandJournalMock.InSequence(sequence).Setup(x => x.MarkResolved(_operationId, true, null));
        _repositoryMock.InSequence(sequence).Setup(x => x.UpdateAsync(order, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.AssignAsync(order.Id, Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _commandJournalMock.Verify(x => x.MarkResolved(_operationId, true, null), Times.Once);
        _repositoryMock.Verify(x => x.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignAsync_ShouldResolveCommandAsFailed_WhenDomainRuleRejectsIt()
    {
        var order = new WorkOrder(Guid.NewGuid(), "Test Order");
        order.Cancel("no longer needed");
        _repositoryMock.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await _sut.AssignAsync(order.Id, Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _commandJournalMock.Verify(x => x.ResolveNowAsync(_operationId, false, "DOMAIN_VALIDATION_FAILED", It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
