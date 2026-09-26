using Voltflow.Api.Errors;
using Voltflow.Application.Interfaces;
using Voltflow.Api.Security;

namespace Voltflow.Api.Endpoints;

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder routes)
    {
        var operations = routes.MapGroup("/api/operations").WithTags("03-WorkOrderExecution").RequireAuthorization("AdminOnly");

        operations.MapGet("/outbox/dead-letter", async (IOutboxRepository repository, CancellationToken ct) =>
            Results.Ok(await repository.ListDeadLetterAsync(ct)))
        .WithName("VF-03501_ListDeadLetterOutbox");

        operations.MapPost("/outbox/{id:guid}/replay", async (Guid id, IOutboxRepository repository, CancellationToken ct) =>
        {
            await repository.RequeueAsync(id, ct);
            return Results.NoContent();
        })
        .WithName("VF-03501_ReplayDeadLetterOutbox")
        .UseMutationPolicy();

        return routes;
    }
}