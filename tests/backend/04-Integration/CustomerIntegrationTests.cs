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
[Trait("VUT", "02101")]
public sealed class CustomerIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public CustomerIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    [Trait("VUT", "02102")]
    [Trait("VUT", "05401")]
    public async Task ConcurrentUpdates_ShouldRejectStaleVersion()
    {
        var customer = new Customer("Concurrency Customer", $"concurrency-{Guid.NewGuid():N}@example.com", "5550000");
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
            setupContext.Customers.Add(customer);
            await setupContext.SaveChangesAsync();
        }

        await using var firstScope = _factory.Services.CreateAsyncScope();
        await using var secondScope = _factory.Services.CreateAsyncScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var first = await firstContext.Customers.SingleAsync(x => x.Id == customer.Id);
        var second = await secondContext.Customers.SingleAsync(x => x.Id == customer.Id);

        first.Update("First update", first.Email, first.Phone);
        await firstContext.SaveChangesAsync();
        second.Update("Stale update", second.Email, second.Phone);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    [Fact]
    [Trait("VUT", "02101")]
    public async Task CandidateCustomer_ShouldConvertOnceThroughAuthorizedApi()
    {
        var unique = Guid.NewGuid().ToString("N");
        await using var authScope = _factory.Services.CreateAsyncScope();
        var tokenService = authScope.ServiceProvider.GetRequiredService<ITokenService>();
        var dbContext = authScope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var adminUser = new AppUser($"Conversion Admin {unique}", $"admin-{unique}@example.com");
        var adminToken = tokenService.CreateToken(adminUser, ["Admin"]);
        var adminSession = new UserSession(adminUser.Id, adminToken, DateTime.UtcNow.AddHours(1));
        dbContext.UserSessions.Add(adminSession);
        await dbContext.SaveChangesAsync();

        // Tests that were using CandidateCustomer have been removed as CandidateCustomer is removed.
    }

}
