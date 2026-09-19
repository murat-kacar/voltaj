using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        // The framework could not read the request (a body that is not JSON, a value of the wrong type, a body that is too large):
        // the exception knows which status that is. What it says names parameters and types, so that stays in the log.
        BadHttpRequestException badRequest => (badRequest.StatusCode, "invalid_request", "Invalid request", "The request is not valid."),
        // a DbUpdateConcurrencyException is a DbUpdateException too, so it has to come before the more general case below
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "concurrency_conflict", "Concurrency conflict", "The resource changed before this operation completed."),
        // Two requests wrote the same unique value at once, such as the very first document of a series. The message of the
        // database names its tables and constraints, so it stays in the log.
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => (StatusCodes.Status409Conflict, "unique_violation", "Duplicate value", "The operation conflicts with a record that already exists."),
        InvalidOperationException => (StatusCodes.Status409Conflict, "business_rule_violation", "Operation cannot be completed", exception.Message),
        OperationCanceledException => (499, "request_cancelled", "Request cancelled", "The operation was cancelled."),
        _ => (StatusCodes.Status500InternalServerError, "internal_error", "Unexpected error", "The operation could not be completed. Use the operation id when contacting support.")
    };
}
