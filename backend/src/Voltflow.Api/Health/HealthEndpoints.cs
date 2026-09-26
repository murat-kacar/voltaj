using Microsoft.EntityFrameworkCore;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Api.Health;

/// <summary>T6: three separate probes with three different questions - never one endpoint doing all of them.</summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        // Liveness: the process is up. Deliberately checks no dependency, so a database outage
        // never gets the process restarted.
        routes.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        // Readiness: it can serve traffic right now - the database is reachable.
        routes.MapGet("/ready", async (VoltflowDbContext dbContext) =>
        {
            var databaseReady = await dbContext.Database.CanConnectAsync();
            return databaseReady
                ? Results.Ok(new { status = "ready" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        });

        // Startup: one-time initialization has finished and the schema is up to date.
        routes.MapGet("/startup", async (StartupState startup, VoltflowDbContext dbContext, CancellationToken ct) =>
        {
            var probe = await StartupProbe.EvaluateAsync(
                startup,
                async token => dbContext.Database.IsRelational() && (await dbContext.Database.GetPendingMigrationsAsync(token)).Any(),
                ct);
            return probe.Started
                ? Results.Ok(new { status = probe.Status })
                : Results.Json(new { status = probe.Status }, statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        return routes;
    }
}
