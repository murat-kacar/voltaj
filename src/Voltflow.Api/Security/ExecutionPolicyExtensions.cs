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
                var services = invocationContext.HttpContext.RequestServices;
                var guard = services.GetRequiredService<Voltflow.Application.Interfaces.IExecutionGuard>();
                var limiter = services.GetRequiredService<Voltflow.Application.Interfaces.IDistributedRateLimiter>();
                var journal = services.GetRequiredService<Voltflow.Application.Interfaces.ICommandJournal>();
                var operationContext = services.GetRequiredService<Voltflow.Application.Interfaces.IOperationContext>();
                var currentUser = services.GetRequiredService<Voltflow.Application.Interfaces.ICurrentUser>();

                var guardFilter = new ExecutionGuardFilter(guard, policy, lifetime ?? TimeSpan.FromDays(3650));
                var rateFilter = new DistributedRateLimitFilter(limiter);
                var commandFilter = new CommandAuditFilter(journal, operationContext, currentUser);

                return await rateFilter.InvokeAsync(invocationContext,
                    context => guardFilter.InvokeAsync(context,
                        innerContext => commandFilter.InvokeAsync(innerContext, next)));
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
