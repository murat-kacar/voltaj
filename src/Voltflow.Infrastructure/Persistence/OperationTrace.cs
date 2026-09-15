using Voltflow.Domain.Common;

namespace Voltflow.Infrastructure.Persistence;

public sealed class OperationTrace : Entity
{
    public Guid OperationId { get; private set; }
    public Guid? ParentOperationId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string? Screen { get; private set; }
    public string? Action { get; private set; }
    public string? QueryString { get; private set; }
    public string? RequestFingerprint { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public long DurationMilliseconds { get; private set; }
    public string? ErrorType { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CompletedAt { get; private set; }

    private OperationTrace() { }

    public OperationTrace(Guid operationId, Guid? parentOperationId, Guid? userId, string endpoint, string? screen, string? action, string? queryString, string? requestFingerprint)
    {
        OperationId = operationId;
        ParentOperationId = parentOperationId;
        UserId = userId;
        Endpoint = Guard.NotEmpty(endpoint, nameof(endpoint));
        Screen = screen;
        Action = action;
        QueryString = queryString;
        RequestFingerprint = requestFingerprint;
    }

    public void Complete(string outcome, int statusCode, long durationMilliseconds, Exception? exception = null)
    {
        Outcome = Guard.NotEmpty(outcome, nameof(outcome));
        StatusCode = statusCode;
        DurationMilliseconds = durationMilliseconds;
        ErrorType = exception?.GetType().Name;
        ErrorMessage = exception?.Message;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }
}