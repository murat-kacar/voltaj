using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Security;

public enum ExecutionPolicy
{
    SameArguments,
    OnceEver
}

public sealed class ExecutionGuardFilter : IEndpointFilter
{
    private readonly IExecutionGuard _guard;
    private readonly ExecutionPolicy _policy;
    private readonly TimeSpan _lifetime;

    public ExecutionGuardFilter(IExecutionGuard guard, ExecutionPolicy policy, TimeSpan lifetime)
    {
        _guard = guard;
        _policy = policy;
        _lifetime = lifetime;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var scope = context.HttpContext.Request.Path.Value ?? "unknown";
        var requestHash = ComputeHash(context.Arguments
            .Where(argument => argument is not null)
            .Where(argument => argument is not CancellationToken && argument is not HttpContext)
            .Where(argument => !IsDependency(argument!.GetType()))
            .Select(argument => JsonSerializer.Serialize(argument)));
        var explicitKey = context.HttpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        var key = _policy == ExecutionPolicy.OnceEver
            ? "once"
            : explicitKey ?? requestHash;

        var acquired = await _guard.TryAcquireAsync(scope, key, requestHash, _lifetime, context.HttpContext.RequestAborted);
        if (acquired != ExecutionGuardAcquireResult.Acquired)
        {
            var message = acquired == ExecutionGuardAcquireResult.RequestHashMismatch
                ? "The idempotency key was already used with different request data."
                : acquired == ExecutionGuardAcquireResult.InProgress
                    ? "This operation is already in progress."
                    : "This operation has already been completed.";
            if (acquired == ExecutionGuardAcquireResult.AlreadyResolved)
            {
                var replay = await _guard.GetResolvedResponseAsync(scope, key, context.HttpContext.RequestAborted);
                if (replay is not null && !string.IsNullOrWhiteSpace(replay.Value.ResponseBody))
                    return Results.Content(replay.Value.ResponseBody, "application/json", Encoding.UTF8, replay.Value.StatusCode);
            }

            return Results.Conflict(new { error = message });
        }

        try
        {
            var result = await next(context);
            var (statusCode, responseBody) = SerializeResult(result);
            if (statusCode >= 200 && statusCode < 300)
            {
                await _guard.ResolveAsync(scope, key, statusCode, responseBody, context.HttpContext.RequestAborted);
            }
            else
            {
                await _guard.OrphanAsync(scope, key, context.HttpContext.RequestAborted);
            }
            return result;
        }
        catch
        {
            await _guard.OrphanAsync(scope, key, CancellationToken.None);
            throw;
        }
    }

    private static string ComputeHash(IEnumerable<string> values)
    {
        var payload = string.Join("|", values);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static (int StatusCode, string? ResponseBody) SerializeResult(object? result)
    {
        if (result is null) return (StatusCodes.Status204NoContent, null);
        var type = result.GetType();
        var statusCode = type.GetProperty("StatusCode")?.GetValue(result) as int? ?? StatusCodes.Status200OK;
        var value = type.GetProperty("Value")?.GetValue(result) ?? result;
        return (statusCode, JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    private static bool IsDependency(Type type)
        => type.Namespace?.StartsWith("Voltflow.Application.Interfaces", StringComparison.Ordinal) == true
           || type.Namespace?.StartsWith("Microsoft.", StringComparison.Ordinal) == true;
}