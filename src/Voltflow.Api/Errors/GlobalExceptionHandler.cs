using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetails,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var mapped = Map(exception);
        var operationContext = httpContext.RequestServices.GetService<IOperationContext>();
        _logger.LogError(exception,
            "Unhandled request failure. OperationId={OperationId} ErrorCode={ErrorCode} StatusCode={StatusCode}",
            operationContext?.OperationId, mapped.Code, mapped.StatusCode);

        httpContext.Response.StatusCode = mapped.StatusCode;
        await _problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = mapped.StatusCode,
                Title = mapped.Title,
                Detail = mapped.Detail,
                Type = $"https://voltflow.dev/problems/{mapped.Code.ToLowerInvariant()}",
                Instance = httpContext.Request.Path
            }
        });
        return true;
    }

    private static (int StatusCode, string Code, string Title, string Detail) Map(Exception exception) => exception switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest, "invalid_argument", "Invalid request", exception.Message),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found", "Resource not found", exception.Message),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "unauthorized", "Unauthorized", "Authentication is required."),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "concurrency_conflict", "Concurrency conflict", "The resource changed before this operation completed."),
        InvalidOperationException => (StatusCodes.Status409Conflict, "business_rule_violation", "Operation cannot be completed", exception.Message),
        OperationCanceledException => (499, "request_cancelled", "Request cancelled", "The operation was cancelled."),
        _ => (StatusCodes.Status500InternalServerError, "internal_error", "Unexpected error", "The operation could not be completed. Use the operation id when contacting support.")
    };
}
