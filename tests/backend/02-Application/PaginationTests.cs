using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Application.Services;
using Voltflow.Domain.Customers;

namespace Voltflow.Tests.Application;

public class PaginationDefaultsTests
{
    [Theory]
    [InlineData(null, PaginationDefaults.DefaultLimit)]
    [InlineData(0, PaginationDefaults.DefaultLimit)]
    [InlineData(-5, PaginationDefaults.DefaultLimit)]
    [InlineData(30, 30)]
    [InlineData(1000, PaginationDefaults.MaxLimit)]
    public void NormalizeLimit_ShouldClampToCeilingAndDefault(int? requested, int expected)
        => PaginationDefaults.NormalizeLimit(requested).Should().Be(expected);

    [Theory]
    [InlineData(null, 0)]
    [InlineData(-10, 0)]
    [InlineData(20, 20)]
    public void NormalizeOffset_ShouldClampToZero(int? requested, int expected)
        => PaginationDefaults.NormalizeOffset(requested).Should().Be(expected);
}

public class CustomerServicePaginationTests
{
    [Fact]
    public async Task ListAsync_ShouldRequestNormalizedPage_AndMapItems()
    {
        var repositoryMock = new Mock<ICustomerRepository>();
        var customer = new Customer("Jane Doe", "jane@example.com", "555-0100", "TAX-1");
        repositoryMock
            .Setup(x => x.ListPagedAsync(It.IsAny<CustomerFilter>(), PaginationDefaults.DefaultLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Customer>(new[] { customer }, TotalCount: 137, Limit: PaginationDefaults.DefaultLimit, Offset: 0));

        var sut = new CustomerService(repositoryMock.Object, new Mock<ICommandJournal>().Object, new Mock<IOperationContext>().Object);

        var result = await sut.ListAsync(limit: null, offset: null, ct: CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(x => x.Id == customer.Id);
        result.Value.TotalCount.Should().Be(137);
        result.Value.HasNext.Should().BeTrue();
        repositoryMock.Verify(x => x.ListPagedAsync(It.IsAny<CustomerFilter>(), PaginationDefaults.DefaultLimit, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_ShouldClampOversizedLimit_BeforeCallingRepository()
    {
        var repositoryMock = new Mock<ICustomerRepository>();
        repositoryMock
            .Setup(x => x.ListPagedAsync(It.IsAny<CustomerFilter>(), PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Customer>(Array.Empty<Customer>(), 0, PaginationDefaults.MaxLimit, 0));

        var sut = new CustomerService(repositoryMock.Object, new Mock<ICommandJournal>().Object, new Mock<IOperationContext>().Object);

        await sut.ListAsync(limit: 5000, offset: null, ct: CancellationToken.None);

        repositoryMock.Verify(x => x.ListPagedAsync(It.IsAny<CustomerFilter>(), PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()), Times.Once);
    }
}
