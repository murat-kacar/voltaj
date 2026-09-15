using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Voltflow.Api.Security;

public static class ExecutionPolicyExtensions
{
    public static RouteHandlerBuilder UseExecutionPolicy(
        this RouteHandlerBuilder builder,
        ExecutionPolicy policy = ExecutionPolicy.SameArguments,
        TimeSpan? lifetime = null)
    {
        return builder.AddEndpointFilterFactory((_, next) =>
            async invocationContext =>
            {
                var guard = invocationContext.HttpContext.RequestServices
                    .GetRequiredService<Voltflow.Application.Interfaces.IExecutionGuard>();
                var limiter = invocationContext.HttpContext.RequestServices
                    .GetRequiredService<Voltflow.Application.Interfaces.IDistributedRateLimiter>();
                var filter = new ExecutionGuardFilter(guard, policy, lifetime ?? TimeSpan.FromDays(3650));
                var rateFilter = new DistributedRateLimitFilter(limiter);
                return await rateFilter.InvokeAsync(invocationContext,
                    context => filter.InvokeAsync(context, next));
            });
    }

    public static RouteHandlerBuilder UseMutationPolicy(this RouteHandlerBuilder builder)
        => builder.UseExecutionPolicy();

    public static RouteHandlerBuilder UseMutationRateLimit(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilterFactory((_, next) =>
            async invocationContext =>
            {
                var limiter = invocationContext.HttpContext.RequestServices
                    .GetRequiredService<Voltflow.Application.Interfaces.IDistributedRateLimiter>();
                return await new DistributedRateLimitFilter(limiter).InvokeAsync(invocationContext, next);
            });
}
