using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Application.Services;
using Voltflow.Domain.Finance;
using Voltflow.Domain.Inventory;
using Voltflow.Shared;

namespace Voltflow.Tests.Application;

/// <summary>The lists of all payments, all invoices and all stock rows: read models, so V8 (a ceiling on every list) is what they answer to.</summary>
public class PaymentListingTests
{
    private readonly Mock<IPaymentRepository> _repository = new();
    private readonly Mock<ICustomerService> _customers = new();
    private readonly PaymentService _sut;

    public PaymentListingTests()
    {
        _sut = new PaymentService(
            _repository.Object, _customers.Object,
            new Mock<ICommandJournal>().Object, new Mock<IOperationContext>().Object);
    }

    private void CustomersAre(params (Guid Id, string Name)[] known)
    {
        var names = new Dictionary<Guid, string>();
        foreach (var (id, name) in known) names[id] = name;
        _customers
            .Setup(x => x.GetNamesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyDictionary<Guid, string>>.Ok(names));
    }

    [Fact]
    public async Task ListAsync_ShouldCarryCustomerNames_AndPassTheCustomerFilterOn()
    {
        var customerId = Guid.NewGuid();
        var payment = new CustomerPayment(customerId, 100m, "Cash", new DateOnly(2026, 1, 15));
        _repository
            .Setup(x => x.ListPagedAsync(customerId, PaginationDefaults.DefaultLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CustomerPayment>(new[] { payment }, TotalCount: 80, Limit: PaginationDefaults.DefaultLimit, Offset: 0));
        CustomersAre((customerId, "Jane Doe"));

        var result = await _sut.ListAsync(customerId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle().Which.CustomerName.Should().Be("Jane Doe");
        result.Value.TotalCount.Should().Be(80);
        result.Value.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_WithoutACustomer_ShouldListEveryones_AndClampAnOversizedLimit()
    {
        _repository
            .Setup(x => x.ListPagedAsync(null, PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CustomerPayment>(Array.Empty<CustomerPayment>(), 0, PaginationDefaults.MaxLimit, 0));
        CustomersAre();

        await _sut.ListAsync(customerId: null, limit: 5000);

        _repository.Verify(x => x.ListPagedAsync(null, PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_ForACustomerThatIsGone_ShouldGiveAnEmptyName_NotFail()
    {
        var payment = new CustomerPayment(Guid.NewGuid(), 50m, "Card", new DateOnly(2026, 2, 1));
        _repository
            .Setup(x => x.ListPagedAsync(null, PaginationDefaults.DefaultLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CustomerPayment>(new[] { payment }, 1, PaginationDefaults.DefaultLimit, 0));
        CustomersAre();

        var result = await _sut.ListAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle().Which.CustomerName.Should().BeEmpty();
    }

    [Fact]
    public async Task ListInvoicesAsync_ShouldCarryCustomerNames_AndPassTheCustomerFilterOn()
    {
        var customerId = Guid.NewGuid();
        var invoice = new SalesInvoice(customerId, "INV-1", 300m, new DateOnly(2026, 1, 15));
        _repository
            .Setup(x => x.ListInvoicesPagedAsync(customerId, PaginationDefaults.DefaultLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SalesInvoice>(new[] { invoice }, 1, PaginationDefaults.DefaultLimit, 0));
        CustomersAre((customerId, "Jane Doe"));

        var result = await _sut.ListInvoicesAsync(customerId);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Items.Should().ContainSingle().Subject;
        row.CustomerName.Should().Be("Jane Doe");
        row.InvoiceNumber.Should().Be("INV-1");
        row.RemainingAmount.Should().Be(300m);
    }

    [Fact]
    public async Task ListInvoicesAsync_ShouldClampAnOversizedLimit_BeforeCallingTheRepository()
    {
        _repository
            .Setup(x => x.ListInvoicesPagedAsync(null, PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SalesInvoice>(Array.Empty<SalesInvoice>(), 0, PaginationDefaults.MaxLimit, 0));
        CustomersAre();

        await _sut.ListInvoicesAsync(limit: 5000);

        _repository.Verify(x => x.ListInvoicesPagedAsync(null, PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class StockListingTests
{
    private readonly Mock<IInventoryRepository> _repository = new();
    private readonly InventoryService _sut;

    public StockListingTests()
    {
        _sut = new InventoryService(_repository.Object, new Mock<ICommandJournal>().Object, new Mock<IOperationContext>().Object);
    }

    [Fact]
    public async Task ListAsync_ShouldPassTheSearchOn_AndMapTheRows()
    {
        var stock = new MaterialStock(Guid.NewGuid(), "CBL-10", "Cable 10 m", 12m);
        _repository
            .Setup(x => x.ListPagedAsync("cable", PaginationDefaults.DefaultLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MaterialStock>(new[] { stock }, TotalCount: 1, Limit: PaginationDefaults.DefaultLimit, Offset: 0));

        var result = await _sut.ListAsync("cable");

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Items.Should().ContainSingle().Subject;
        row.MaterialCode.Should().Be("CBL-10");
        row.QuantityOnHand.Should().Be(12m);
        row.AvailableQuantity.Should().Be(12m);
    }

    [Fact]
    public async Task ListAsync_ShouldClampAnOversizedLimit_AndANegativeOffset()
    {
        _repository
            .Setup(x => x.ListPagedAsync(null, PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MaterialStock>(Array.Empty<MaterialStock>(), 0, PaginationDefaults.MaxLimit, 0));

        await _sut.ListAsync(limit: 5000, offset: -3);

        _repository.Verify(x => x.ListPagedAsync(null, PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()), Times.Once);
    }
}
