using System.Security.Claims;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Security;

public enum RateLimitScope
{
    /// <summary>Everyday work by signed-in users. Generous, so a busy counter or a long work order is never throttled.</summary>
    Business,

    /// <summary>Anonymous entry points (sign-in, registration, password reset). Tight, against guessing and floods.</summary>
    Strict
}

public sealed class DistributedRateLimitFilter : IEndpointFilter
{
    private readonly IDistributedRateLimiter _limiter;
    private readonly RateLimitScope _scope;

    public DistributedRateLimitFilter(IDistributedRateLimiter limiter, RateLimitScope scope)
    {
        _limiter = limiter;
        _scope = scope;
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
            var development = env?.IsDevelopment() == true;
            var limit = _scope == RateLimitScope.Strict
                ? config?.GetValue<int?>("RateLimiting:PermitLimit") ?? (development ? 500 : 5)
                : config?.GetValue<int?>("RateLimiting:MutationPermitLimit") ?? (development ? 500 : 120);

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