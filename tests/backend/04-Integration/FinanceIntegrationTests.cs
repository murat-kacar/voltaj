using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Identity;
using Voltflow.Domain.Reminders;
using Voltflow.Domain.WorkOrders;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Infrastructure.Repositories;
using Voltflow.Worker;

namespace Voltflow.Tests;


[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "05101")]
public sealed class FinanceIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public FinanceIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    [Trait("VUT", "05301")]
    [Trait("VUT", "05302")]
    [Trait("VUT", "05303")]
    public async Task PaymentAllocation_ShouldRejectOverAllocationThroughDomainRule()
    {
        var payment = new Voltflow.Domain.Finance.CustomerPayment(Guid.NewGuid(), 100, "Cash", DateOnly.FromDateTime(DateTime.UtcNow));
        var invoice = new Voltflow.Domain.Finance.SalesInvoice(payment.CustomerId, $"INV-{Guid.NewGuid():N}", 80, DateOnly.FromDateTime(DateTime.UtcNow));

        payment.Allocate(80);
        invoice.Allocate(80);

        Assert.Throws<InvalidOperationException>(() => payment.Allocate(21));
        Assert.Throws<InvalidOperationException>(() => invoice.Allocate(1));
    }

}
