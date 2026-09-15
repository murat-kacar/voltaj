using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Voltflow.Api.Errors;
using Voltflow.Api.Security;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var inventory = routes.MapGroup("/api/inventory").WithTags("04-Inventory");
        inventory.RequireAuthorization("Authenticated");

        inventory.MapGet("{materialCode}", async (string materialCode, IInventoryService service, CancellationToken ct) =>
        {
            var result = await service.GetByMaterialCodeAsync(materialCode, ct);
            return result.NotFound();
        })
        .WithName("VF-04101_GetStockByMaterialCode")
        .RequireAuthorization("OperationsAccess");

        inventory.MapPost("adjust", async (AdjustStockRequest request, IInventoryService service, CancellationToken ct) =>
        {
            var result = await service.AdjustAsync(request, ct);
            return result.From();
        })
        .WithName("VF-04101_AdjustStock")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        inventory.MapPost("reserve", async (ReserveStockRequest request, IInventoryService service, CancellationToken ct) =>
        {
            var result = await service.ReserveAsync(request, ct);
            return result.From();
        })
        .WithName("VF-04201_ReserveStock")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        return routes;
    }
}
