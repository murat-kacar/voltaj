using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Voltflow.Api.Errors;
using Voltflow.Api.Security;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var projects = routes.MapGroup("/api/projects").WithTags("02-QuoteToOrder");
        projects.RequireAuthorization("Authenticated");

        projects.MapGet("", async (IProjectService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(ct);
            return result.From();
        })
        .WithName("VF-02501_ListProjects");

        projects.MapGet("{id:guid}", async (Guid id, IProjectService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result.NotFound();
        })
        .WithName("VF-02501_GetProjectById");

        projects.MapPost("", async (CreateProjectRequest request, IProjectService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.From();
        })
        .WithName("VF-02501_CreateProject")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        projects.MapPost("{id:guid}/phases", async (Guid id, CreateProjectPhaseRequest request, IProjectService service, CancellationToken ct) =>
        {
            var result = await service.AddPhaseAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-02501_AddProjectPhase")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        return routes;
    }
}
