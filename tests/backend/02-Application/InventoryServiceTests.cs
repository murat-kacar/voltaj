using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Voltflow.Application.Services;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Inventory;

namespace Voltflow.Tests.Application;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _repositoryMock;
    private readonly Mock<ICommandJournal> _commandJournalMock;
    private readonly Mock<IOperationContext> _operationContextMock;
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _repositoryMock = new Mock<IInventoryRepository>();
        _commandJournalMock = new Mock<ICommandJournal>();
        _operationContextMock = new Mock<IOperationContext>();
        _sut = new InventoryService(_repositoryMock.Object, _commandJournalMock.Object, _operationContextMock.Object);
    }

    [Fact]
    public async Task GetByMaterialCodeAsync_ShouldReturnStock_WhenFound()
    {
        var stock = new MaterialStock(Guid.NewGuid(), "MAT-01", "Wire", 100m);
        _repositoryMock.Setup(x => x.GetByMaterialCodeAsync("MAT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        var result = await _sut.GetByMaterialCodeAsync("MAT-01", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MaterialCode.Should().Be("MAT-01");
        result.Value.QuantityOnHand.Should().Be(100m);
    }

    [Fact]
    public async Task GetByMaterialCodeAsync_ShouldFail_WhenNotFound()
    {
        _repositoryMock.Setup(x => x.GetByMaterialCodeAsync("MAT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaterialStock?)null);

        var result = await _sut.GetByMaterialCodeAsync("MAT-01", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task AdjustAsync_ShouldReturnAdjustedStock_WhenValid()
    {
        var stock = new MaterialStock(Guid.NewGuid(), "MAT-01", "Wire", 150m); // Started at 100, delta 50
        var request = new AdjustStockRequest("MAT-01", 50m);
        _repositoryMock.Setup(x => x.AdjustWithMovementAsync("MAT-01", 50m, "Manual stock adjustment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        var result = await _sut.AdjustAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QuantityOnHand.Should().Be(150m);
    }

    [Fact]
    public async Task ReserveAsync_ShouldUpdateReservedQuantity_WhenValid()
    {
        var stock = new MaterialStock(Guid.NewGuid(), "MAT-01", "Wire", 100m);
        var request = new ReserveStockRequest("MAT-01", 30m);
        _repositoryMock.Setup(x => x.GetByMaterialCodeAsync("MAT-01", It.IsAny<CancellationToken>())).ReturnsAsync(stock);

        var result = await _sut.ReserveAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReservedQuantity.Should().Be(30m);
        result.Value.AvailableQuantity.Should().Be(70m);
        _repositoryMock.Verify(x => x.UpdateAsync(stock, It.IsAny<CancellationToken>()), Times.Once);
    }
}
