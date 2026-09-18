using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Api.Observability;

namespace Voltflow.Api.Diagnostics;

public sealed class OperationTraceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OperationTraceMiddleware> _logger;
    private readonly ApiMetrics _metrics;

    public OperationTraceMiddleware(RequestDelegate next, ILogger<OperationTraceMiddleware> logger, ApiMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext httpContext, IOperationContext operationContext, VoltflowDbContext dbContext)
    {
        var userId = Guid.TryParse(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? httpContext.User.FindFirstValue("sub"),
            out var parsedUserId) ? parsedUserId : (Guid?)null;
        var parentOperationId = Guid.TryParse(httpContext.Request.Headers["X-Parent-Operation-Id"], out var parsedParent)
            ? parsedParent : (Guid?)null;
        var screen = httpContext.Request.Headers["X-Client-Screen"].FirstOrDefault();
        var action = httpContext.Request.Headers["X-Client-Action"].FirstOrDefault();
        var endpoint = $"{httpContext.Request.Method} {httpContext.Request.Path}";
        var queryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : null;
        var requestFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{httpContext.Request.Method}|{httpContext.Request.Path}|{queryString}|{screen}|{action}")));
        operationContext.Begin(endpoint, screen, action, parentOperationId, userId);
    httpContext.Response.Headers["X-Operation-Id"] = operationContext.OperationId.ToString();

        var trace = new OperationTrace(operationContext.OperationId, parentOperationId, userId, endpoint, screen, action, queryString, requestFingerprint);
        dbContext.OperationTraces.Add(trace);
        var stopwatch = Stopwatch.StartNew();
        // T1: the server span already exists - created by the ASP.NET Core instrumentation with the
        // standard name and http.* / url.* attributes, and parented on an incoming W3C traceparent (T2).
        // This middleware only adds Voltflow-specific attributes to it; it must not start a second span.
        var activity = Activity.Current;
        activity?.SetTag("voltflow.operation_id", operationContext.OperationId.ToString());
        if (parentOperationId is not null) activity?.SetTag("voltflow.parent_operation_id", parentOperationId.ToString());
        activity?.SetTag("voltflow.screen", screen);
        activity?.SetTag("voltflow.action", action);
        try
        {
            await _next(httpContext);
        }
        catch (OperationCanceledException exception)
        {
            trace.Complete("CANCELLED", 499, stopwatch.ElapsedMilliseconds, exception);
            activity?.SetTag("voltflow.outcome", "CANCELLED");
            await PersistTraceSafelyAsync(dbContext, httpContext.RequestAborted);
            throw;
        }
        catch (Exception exception)
        {
            trace.Complete("FAILED", 500, stopwatch.ElapsedMilliseconds, exception);
            activity?.SetTag("voltflow.outcome", "FAILED");
            await PersistTraceSafelyAsync(dbContext, CancellationToken.None);
            throw;
        }

        var outcome = httpContext.Response.StatusCode >= 400 ? "BLOCKED_OR_FAILED" : "COMPLETED";
        trace.Complete(outcome, httpContext.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        _metrics.Record(endpoint, httpContext.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        activity?.SetTag("voltflow.outcome", outcome);
        await PersistTraceSafelyAsync(dbContext, httpContext.RequestAborted);
        _logger.LogInformation("Operation {OperationId} {Outcome} {StatusCode} {Endpoint} in {Duration}ms.",
            operationContext.OperationId, outcome, httpContext.Response.StatusCode, endpoint, stopwatch.ElapsedMilliseconds);
    }

    private static async Task PersistTraceSafelyAsync(VoltflowDbContext dbContext, CancellationToken ct)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            // Logging must never replace the original request failure.
        }
    }
}
