using System.Net;
using System.Text.Json;
using Voltflow.Api.Health;

namespace Voltflow.Tests;

/// <summary>T6: liveness, readiness and startup are separate endpoints answering separate questions.</summary>
[Xunit.Collection("ApiIntegration")]
public sealed class HealthProbeIntegrationTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;

    public HealthProbeIntegrationTests(ApiTestFixture fixture) => _client = fixture.CreateApiClient();

    private async Task<(HttpStatusCode Status, string? Body)> ProbeAsync(string path)
    {
        using var response = await _client.GetAsync(path);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (response.StatusCode, document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Liveness_ShouldReportOk() => Assert.Equal((HttpStatusCode.OK, "ok"), await ProbeAsync("/health"));

    [Fact]
    public async Task Readiness_ShouldReportReady() => Assert.Equal((HttpStatusCode.OK, "ready"), await ProbeAsync("/ready"));

    [Fact]
    public async Task Startup_ShouldReportStarted_OnceInitializationHasFinished()
        => Assert.Equal((HttpStatusCode.OK, "started"), await ProbeAsync("/startup"));

    [Fact]
    public async Task Probes_ShouldAnswerThreeDifferentQuestions()
    {
        var statuses = new[] { (await ProbeAsync("/health")).Body, (await ProbeAsync("/ready")).Body, (await ProbeAsync("/startup")).Body };

        Assert.Equal(3, statuses.Distinct().Count());
    }
}

public sealed class StartupProbeTests
{
    [Fact]
    public async Task ShouldReportStarting_AndNotTouchTheDatabase_BeforeInitializationHasFinished()
    {
        var queried = false;

        var result = await StartupProbe.EvaluateAsync(new StartupState(), _ => { queried = true; return Task.FromResult(false); });

        Assert.Equal((false, "starting"), result);
        Assert.False(queried);
    }

    [Fact]
    public async Task ShouldReportMigrationsPending_WhenTheSchemaIsBehind()
    {
        var state = new StartupState();
        state.MarkCompleted();

        var result = await StartupProbe.EvaluateAsync(state, _ => Task.FromResult(true));

        Assert.Equal((false, "migrations_pending"), result);
    }

    [Fact]
    public async Task ShouldReportStarted_WhenInitializedAndUpToDate()
    {
        var state = new StartupState();
        state.MarkCompleted();

        var result = await StartupProbe.EvaluateAsync(state, _ => Task.FromResult(false));

        Assert.Equal((true, "started"), result);
    }
}
