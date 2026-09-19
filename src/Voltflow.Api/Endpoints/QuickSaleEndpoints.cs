using Voltflow.Api.Errors;
using Voltflow.Api.Pagination;
using Voltflow.Api.Security;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Endpoints;

public static class QuickSaleEndpoints
{
    public static IEndpointRouteBuilder MapQuickSaleEndpoints(this IEndpointRouteBuilder routes)
    {
        // Anyone who works in the field can sell; taking a sale back (void or return) is a manager's call.
        var sales = routes.MapGroup("/api/quick-sales").WithTags("09-QuickSale");
        sales.RequireAuthorization("OperationsAccess");

        sales.MapPost("", async (CreateQuickSaleRequest request, IQuickSaleService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.From();
        })
        .WithName("VF-09101_CreateQuickSale")
        .UseMutationPolicy();

        sales.MapGet("", async (string? search, string? status, DateTimeOffset? from, DateTimeOffset? to, int? limit, int? offset, HttpContext httpContext, IQuickSaleService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(search, status, from?.UtcDateTime, to?.UtcDateTime, limit, offset, ct);
            if (result.IsSuccess) httpContext.ApplyPaginationHeaders(result.Value!);
            return result.From(page => Results.Ok(page.Items));
        })
        .WithName("VF-09102_ListQuickSales");

        sales.MapGet("{id:guid}", async (Guid id, IQuickSaleService service, CancellationToken ct) =>
        {
            var result = await service.GetAsync(id, ct);
            return result.NotFound();
        })
        .WithName("VF-09103_GetQuickSale");

        sales.MapPost("{id:guid}/void", async (Guid id, VoidQuickSaleRequest request, IQuickSaleService service, CancellationToken ct) =>
        {
            var result = await service.VoidAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-09104_VoidQuickSale")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        sales.MapPost("{id:guid}/returns", async (Guid id, ReturnQuickSaleRequest request, IQuickSaleService service, CancellationToken ct) =>
        {
            var result = await service.ReturnAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-09105_ReturnQuickSale")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        return routes;
    }
}
