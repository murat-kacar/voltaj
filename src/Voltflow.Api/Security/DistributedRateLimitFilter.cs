using System.Security.Claims;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Security;

public sealed class DistributedRateLimitFilter : IEndpointFilter
{
    private readonly IDistributedRateLimiter _limiter;

    public DistributedRateLimitFilter(IDistributedRateLimiter limiter)
    {
        _limiter = limiter;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        var partition = $"{httpContext.Request.Path}|{user}";
        RateLimitDecision decision;
        try
        {
            var env = httpContext.RequestServices.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            var config = httpContext.RequestServices.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            var defaultLimit = env?.IsDevelopment() == true ? 500 : 5;
            var limit = config?.GetValue<int?>("RateLimiting:PermitLimit") ?? defaultLimit;

            decision = await _limiter.CheckAsync(partition, limit, TimeSpan.FromMinutes(1), httpContext.RequestAborted);
        }
        catch
        {
            // Graceful degradation: when distributed cache/limiter is offline, allow mutation
            decision = new RateLimitDecision(true, 0);
        }

        if (!decision.Allowed)
        {
            httpContext.Response.Headers.RetryAfter = decision.RetryAfterSeconds.ToString();
            return Results.Problem(statusCode: 429, title: "Too many requests", detail: "Mutation rate limit exceeded.", type: "https://voltflow.dev/problems/rate-limit-exceeded");
        }

        return await next(context);
    }
}