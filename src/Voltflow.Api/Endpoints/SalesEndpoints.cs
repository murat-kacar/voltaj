using Microsoft.AspNetCore.Mvc;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Api.Errors;
using Voltflow.Api.Pagination;

namespace Voltflow.Api.Endpoints;

public static class SalesEndpoints
{
    public static void MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sales").WithTags("Sales").RequireAuthorization();

        group.MapPost("/quick", async ([FromBody] StartQuickSaleRequest request, IQuickSaleService service, CancellationToken ct) =>
        {
            var result = await service.CompleteSaleAsync(request, ct);
            return result.From();
        });

        group.MapPost("/quick/{id:guid}/void", async (Guid id, [FromBody] VoidQuickSaleRequest request, IQuickSaleService service, CancellationToken ct) =>
        {
            var result = await service.VoidSaleAsync(id, request.Reason, ct);
            return result.From();
        });

        group.MapGet("/quick", async (int? limit, int? offset, HttpContext httpContext, IQuickSaleService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(limit, offset, ct);
            if (result.IsSuccess) httpContext.ApplyPaginationHeaders(result.Value!);
            return result.From(page => Results.Ok(page.Items));
        });

        group.MapGet("/quick/{id:guid}", async (Guid id, IQuickSaleService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result.NotFound();
        });
    }
}
