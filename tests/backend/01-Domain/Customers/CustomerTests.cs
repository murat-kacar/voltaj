using System;
using FluentAssertions;
using Voltflow.Domain.Customers;
using Xunit;

namespace Voltflow.Tests.Domain.Customers;

public class CustomerTests
{
    [Fact]
    public void Create_TrimsTheDetails_AndTheEmailIsOptional()
    {
        var customer = new Customer("  Ali Yılmaz ", null, " 0532 111 22 33 ", " 1234567890 ");

        customer.FullName.Should().Be("Ali Yılmaz");
        customer.Email.Should().BeEmpty();
        customer.Phone.Should().Be("0532 111 22 33");
        customer.TaxNumber.Should().Be("1234567890");
        customer.Type.Should().Be(CustomerType.Lead);
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_NeedsANameAndAPhone()
    {
        var noName = () => new Customer(" ", "a@example.com", "555", null);
        var noPhone = () => new Customer("Ali", "a@example.com", " ", null);

        noName.Should().Throw<ArgumentException>();
        noPhone.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesTheDetails_TaxNumberIncluded()
    {
        var customer = new Customer("Ali", "a@example.com", "555", "111");

        customer.Update(" Ali Veli ", null, " 556 ", " 222 ");

        (customer.FullName, customer.Email, customer.Phone, customer.TaxNumber).Should().Be(("Ali Veli", "", "556", "222"));
    }

    [Fact]
    public void Update_ChangesNothing_WhenTheNameOrThePhoneIsMissing()
    {
        var customer = new Customer("Ali", "a@example.com", "555", "111");

        var noName = () => customer.Update(" ", "b@example.com", "556", "222");
        var noPhone = () => customer.Update("Veli", "b@example.com", " ", "222");

        noName.Should().Throw<ArgumentException>();
        noPhone.Should().Throw<ArgumentException>();
        (customer.FullName, customer.Email, customer.Phone, customer.TaxNumber).Should().Be(("Ali", "a@example.com", "555", "111"));
    }

    [Fact]
    public void TheOlderUpdate_LeavesTheTaxNumberAlone()
    {
        var customer = new Customer("Ali", "a@example.com", "555", "111");

        customer.Update("Ali Veli", "b@example.com", "556");

        customer.TaxNumber.Should().Be("111");
    }

    [Fact]
    public void ConvertToActive_WorksOnce()
    {
        var customer = new Customer("Ali", "a@example.com", "555");

        customer.ConvertToActive();
        var again = () => customer.ConvertToActive();

        customer.Type.Should().Be(CustomerType.Active);
        again.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ACustomer_CanBeSetInactiveAndActiveAgain()
    {
        var customer = new Customer("Ali", "a@example.com", "555");

        customer.SetInactive();
        customer.IsActive.Should().BeFalse();
        customer.SetActive();
        customer.IsActive.Should().BeTrue();
    }
}
