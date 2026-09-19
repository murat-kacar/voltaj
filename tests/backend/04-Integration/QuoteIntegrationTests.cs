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
[Trait("VUT", "02203")]
public sealed class QuoteIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public QuoteIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    [Trait("VUT", "02401")]
    [Trait("VUT", "02501")]
    public async Task AcceptedQuote_ShouldCreateWorkOrderThroughApi()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var customer = new Customer("Quote customer", $"quote-{Guid.NewGuid():N}@example.com", "5553000");
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var admin = new AppUser($"Quote Admin {Guid.NewGuid():N}", $"quote-admin-{Guid.NewGuid():N}@example.com");
        var token = tokenService.CreateToken(admin, ["Admin"]);
        dbContext.UserSessions.Add(new UserSession(admin.Id, token, DateTime.UtcNow.AddHours(1)));
        await dbContext.SaveChangesAsync();

        async Task<HttpResponseMessage> PostAsync(string url, object? body = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await _client.SendAsync(request);
        }

        using var createResponse = await PostAsync("/api/quotes", new CreateQuoteRequest(customer.Id, "Installation quote"));
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var quote = await createResponse.Content.ReadFromJsonAsync<QuoteDto>();
        Assert.NotNull(quote);

        using var itemResponse = await PostAsync($"/api/quotes/{quote.Id}/items", new QuoteLineRequest("Installation", 2, 150));
        Assert.Equal(HttpStatusCode.OK, itemResponse.StatusCode);
        using var issueResponse = await PostAsync($"/api/quotes/{quote.Id}/issue");
        Assert.Equal(HttpStatusCode.OK, issueResponse.StatusCode);
        using var acceptResponse = await PostAsync($"/api/quotes/{quote.Id}/accept", new AcceptQuoteRequest(null));
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        using var workOrderResponse = await PostAsync($"/api/quotes/{quote.Id}/work-order");
        Assert.Equal(HttpStatusCode.OK, workOrderResponse.StatusCode);
        var workOrder = await workOrderResponse.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.NotNull(workOrder);
        Assert.Equal(300, workOrder.Total);
    }

}
