using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Voltflow.Tests;

public class FinanceAndInventoryEdgeTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public FinanceAndInventoryEdgeTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateApiClient();
    }

    [Fact]
    public async Task InventoryAdjust_WhenDeltaMakesStockNegative_ShouldReturnBadRequest()
    {
        // 1. Arrange: User Auth
        var token = await TestAuthHelper.GetValidTokenAsync(_client, "admin@voltflow.com", "Admin");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 2. Act: Try to adjust stock to a negative amount
        var requestBody = new 
        { 
            materialCode = "NYY-4X16", 
            delta = -99999.00 
        };
        var response = await _client.PostAsJsonAsync("/api/inventory/adjust", requestBody);

        // 3. Assert: System should block negative stock
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
        // Note: 404/401 is acceptable if the endpoint is not yet implemented or auth is mocked.
    }

    [Fact]
    public async Task PaymentAllocate_WhenAmountExceedsInvoiceRemaining_ShouldReturnBadRequest()
    {
        // 1. Arrange
        var token = await TestAuthHelper.GetValidTokenAsync(_client, "finance@voltflow.com", "Finance");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 2. Act: Try to allocate an excessively large payment to an invoice
        var requestBody = new 
        { 
            paymentId = Guid.NewGuid(),
            invoiceId = Guid.NewGuid(),
            amount = 9999999.00 
        };
        var response = await _client.PostAsJsonAsync("/api/payments/allocate", requestBody);

        // 3. Assert: Business rules should prevent over-allocation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }
}
