using System.Text.Json;
using Voltflow.Application.Commands;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Security;

/// <summary>
/// M11 + H9: captures the Command verbatim and writes it `pending` in its own transaction before the
/// handler runs. If the handler's own service resolves it as part of its business transaction (see
/// WorkOrderService.ExecuteActionAsync for the pattern), this filter does nothing further. If it is
/// still `pending` after the handler returns - the common case for services that haven't adopted the
/// piggyback pattern yet - this filter resolves it itself in an immediate follow-up transaction, so
/// every mutation gets real H9 crash-detection today, with same-transaction atomicity as a per-service
/// upgrade path.
/// </summary>
public sealed class CommandAuditFilter : IEndpointFilter
{
    private readonly ICommandJournal _journal;
    private readonly IOperationContext _operationContext;
    private readonly ICurrentUser _currentUser;

    public CommandAuditFilter(ICommandJournal journal, IOperationContext operationContext, ICurrentUser currentUser)
    {
        _journal = journal;
        _operationContext = operationContext;
        _currentUser = currentUser;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var commandType = context.HttpContext.GetEndpoint()?.Metadata
            .GetMetadata<Microsoft.AspNetCore.Routing.IEndpointNameMetadata>()?.EndpointName
            ?? _operationContext.Endpoint;

        var payload = context.Arguments
            .Where(argument => argument is not null)
            .Where(argument => argument is not CancellationToken && argument is not HttpContext)
            .Where(argument => !IsDependency(argument!.GetType()))
            .ToList();
        var payloadJson = JsonSerializer.Serialize(payload);

        var triggerSource = _operationContext.Screen is not null || _operationContext.Action is not null
            ? $"{_operationContext.Endpoint} (screen={_operationContext.Screen}, action={_operationContext.Action})"
            : _operationContext.Endpoint;

        var command = new Command(
            _operationContext.OperationId,
            _operationContext.ParentOperationId,
            _operationContext.UserId,
            _currentUser.Roles,
            triggerSource,
            commandType,
            payloadJson,
            DateTime.UtcNow);

        await _journal.BeginAsync(command, context.HttpContext.RequestAborted);

        object? result;
        try
        {
            result = await next(context);
        }
        catch
        {
            await ResolveIfStillPendingAsync(command.CommandId, success: false, errorCode: "UNHANDLED_EXCEPTION", context.HttpContext.RequestAborted);
            throw;
        }

        var statusCode = GetStatusCode(result);
        var success = statusCode is >= 200 and < 300;
        await ResolveIfStillPendingAsync(command.CommandId, success, success ? null : $"HTTP_{statusCode}", context.HttpContext.RequestAborted);
        return result;
    }

    private async Task ResolveIfStillPendingAsync(Guid commandId, bool success, string? errorCode, CancellationToken ct)
    {
        if (await _journal.IsPendingAsync(commandId, ct))
        {
            await _journal.ResolveNowAsync(commandId, success, errorCode, ct);
        }
    }

    private static int GetStatusCode(object? result)
    {
        if (result is null) return StatusCodes.Status204NoContent;
        return result.GetType().GetProperty("StatusCode")?.GetValue(result) as int? ?? StatusCodes.Status200OK;
    }

    private static bool IsDependency(Type type)
        => type.Namespace?.StartsWith("Voltflow.Application.Interfaces", StringComparison.Ordinal) == true
           || type.Namespace?.StartsWith("Microsoft.", StringComparison.Ordinal) == true;
}
