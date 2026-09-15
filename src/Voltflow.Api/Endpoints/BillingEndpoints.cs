using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Voltflow.Api.Errors;
using Voltflow.Api.Security;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder routes)
    {
        var billing = routes.MapGroup("/api/billing").WithTags("05-Finance");
        billing.RequireAuthorization("Authenticated");

        billing.MapGet("{projectId:guid}", async (Guid projectId, IBillingService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(projectId, ct);
            return result.From();
        })
        .WithName("VF-05101_ListBillingEntries");

        billing.MapPost("{projectId:guid}", async (Guid projectId, CreateBillingEntryRequest request, IBillingService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(projectId, request, ct);
            return result.From();
        })
        .WithName("VF-05101_CreateBillingEntry")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        return routes;
    }
}
