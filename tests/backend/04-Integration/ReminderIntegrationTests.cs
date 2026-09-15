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
public sealed class ReminderIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public ReminderIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }
    [Fact]
    public async Task ReminderProcessor_ShouldPersistDueAttemptAndRetrySchedule()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var reminder = new ReminderRecord(
            "PAYMENT_DUE",
            "Invoice",
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(-1),
            "Payment due");
        dbContext.ReminderRecords.Add(reminder);
        await dbContext.SaveChangesAsync();

        var processor = new ReminderProcessor(new ReminderRepository(dbContext));
        var processed = await processor.ProcessDueAsync(DateTime.UtcNow);

        Assert.True(processed >= 1);
        var persisted = await dbContext.ReminderRecords.SingleAsync(x => x.Id == reminder.Id);
        Assert.Equal(1, persisted.Attempts);
        Assert.Equal(ReminderState.Pending, persisted.State);
        Assert.True(persisted.NextAttemptAt > DateTime.UtcNow);
    }

}
