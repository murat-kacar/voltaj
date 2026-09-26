using Voltflow.Api.Errors;
using Voltflow.Api.Pagination;
using Voltflow.Api.Security;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/products").WithTags("04-Inventory");
        group.RequireAuthorization("OperationsAccess");

        group.MapGet("", async (string? search, bool? activeOnly, int? limit, int? offset, HttpContext httpContext, IProductService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(search, activeOnly ?? false, limit, offset, ct);
            if (result.IsSuccess) httpContext.ApplyPaginationHeaders(result.Value!);
            return result.From(page => Results.Ok(page.Items));
        })
        .WithName("VF-04301_ListProducts");

        group.MapGet("lookup", async (string term, IProductService service, CancellationToken ct) =>
        {
            var result = await service.LookupAsync(term, ct);
            return result.NotFound();
        })
        .WithName("VF-04302_LookupProduct");

        group.MapPost("", async (CreateProductRequest request, IProductService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.From();
        })
        .WithName("VF-04303_CreateProduct")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPut("{id:guid}", async (Guid id, UpdateProductRequest request, IProductService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-04304_UpdateProduct")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        return routes;
    }
}
