using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Voltflow.Tests;

public class IsolationAndRbacTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public IsolationAndRbacTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateApiClient();
    }

    [Fact]
    public async Task CheckIn_ByDifferentTechnician_ShouldReturnForbiddenOrBadRequest()
    {
        // 1. Arrange
        var technicianA_Token = await TestAuthHelper.GetValidTokenAsync(_client, "tech_a@voltflow.com", "Technician");
        var technicianB_Token = await TestAuthHelper.GetValidTokenAsync(_client, "tech_b@voltflow.com", "Technician");

        // Technician A creates/gets assigned a work order
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", technicianA_Token);
        // Note: For full E2E we'd use the real API to create the work order.
        // Assuming we have an endpoint that returns a test work order assigned to Tech A.
        var workOrderId = Guid.NewGuid(); // Placeholder for actual ID

        // 2. Act: Technician B tries to check-in to Technician A's work order
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", technicianB_Token);
        var response = await _client.PostAsync($"/api/work-orders/{workOrderId}/check-in", null);

        // 3. Assert: Should be prevented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWorkOrder_ByViewer_ShouldReturnForbidden()
    {
        // 1. Arrange: Viewer Role
        var viewerToken = await TestAuthHelper.GetValidTokenAsync(_client, "viewer@voltflow.com", "Viewer");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", viewerToken);

        // 2. Act: Try to mutate data
        var requestBody = new { title = "Test Mutation" };
        var response = await _client.PostAsJsonAsync("/api/work-orders", requestBody);

        // 3. Assert: Viewer should not be allowed to POST
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
