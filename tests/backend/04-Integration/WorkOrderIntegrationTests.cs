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
[Trait("VUT", "03101")]
public sealed class WorkOrderIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public WorkOrderIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    [Trait("VUT", "03102")]
    public async Task Technician_ShouldOnlySeeAssignedWorkOrders()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var technician = new AppUser($"Technician {Guid.NewGuid():N}", $"technician-{Guid.NewGuid():N}@example.com");
        var otherTechnician = Guid.NewGuid();
        var customer = new Customer("Technician customer", $"tech-customer-{Guid.NewGuid():N}@example.com", "5554000");
        var assigned = new WorkOrder(customer.Id, "Assigned work");
        assigned.Assign(technician.Id);
        var unassigned = new WorkOrder(customer.Id, "Other work");
        unassigned.Assign(otherTechnician);
        dbContext.AddRange(customer, assigned, unassigned);
        await dbContext.SaveChangesAsync();

        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().CreateToken(technician, ["Technician"]);
        dbContext.UserSessions.Add(new UserSession(technician.Id, token, DateTime.UtcNow.AddHours(1)));
        await dbContext.SaveChangesAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/workorders");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<WorkOrderDto>>();
        Assert.NotNull(orders);
        Assert.Contains(orders, order => order.Id == assigned.Id);
        Assert.DoesNotContain(orders, order => order.Id == unassigned.Id);
    }

}
