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
using Voltflow.Infrastructure.Persistence;
using Voltflow.Infrastructure.Repositories;
using Voltflow.Worker;

namespace Voltflow.Tests;


[Xunit.Collection("ApiIntegration")]
public sealed class HealthIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public HealthIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }
    [Fact]
    public async Task Health_ShouldReturnOperationCorrelationHeader()
    {
        using var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Operation-Id"));
        Assert.True(Guid.TryParse(response.Headers.GetValues("X-Operation-Id").Single(), out _));
    }

}
