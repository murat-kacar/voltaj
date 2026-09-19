using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Voltflow.Api.Errors;
using Voltflow.Api.Security;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Endpoints;

public static class AuditLogEndpoints
{
    public static IEndpointRouteBuilder MapAuditLogEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/audit-logs").WithTags("99-Audit");
        group.RequireAuthorization("Authenticated");

        group.MapPost("", async (CreateAuditLogRequest request, IAuditLogService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.From();
        })
        .WithName("VF-99101_CreateAuditLog")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapGet("recent", async (IAuditLogService service, CancellationToken ct) =>
        {
            var result = await service.ListRecentAsync(50, ct);
            return result.From();
        })
        .WithName("VF-99201_ListRecentLogs")
        .RequireAuthorization("AdminOnly");

        group.MapGet("entity/{entityName}/{entityId}", async (string entityName, string entityId, IAuditLogService service, CancellationToken ct) =>
        {
            var result = await service.ListByEntityAsync(entityName, entityId, ct);
            return result.From();
        })
        .WithName("VF-99301_ListLogsByEntity")
        .RequireAuthorization("AdminOnly");

        return routes;
    }
}
