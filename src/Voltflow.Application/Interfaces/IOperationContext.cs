namespace Voltflow.Application.Interfaces;

public interface IOperationContext
{
    Guid OperationId { get; }
    Guid? ParentOperationId { get; }
    Guid? UserId { get; }
    string Endpoint { get; }
    string? Screen { get; }
    string? Action { get; }
    void Begin(string endpoint, string? screen, string? action, Guid? parentOperationId, Guid? userId);
}