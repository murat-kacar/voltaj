using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Diagnostics;

public sealed class OperationContext : IOperationContext
{
    public Guid OperationId { get; private set; }
    public Guid? ParentOperationId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string? Screen { get; private set; }
    public string? Action { get; private set; }

    public void Begin(string endpoint, string? screen, string? action, Guid? parentOperationId, Guid? userId)
    {
        OperationId = Guid.NewGuid();
        Endpoint = endpoint;
        Screen = screen;
        Action = action;
        ParentOperationId = parentOperationId;
        UserId = userId;
    }
}
