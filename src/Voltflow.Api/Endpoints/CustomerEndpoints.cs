using Microsoft.AspNetCore.Http;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Api.Security;
using Voltflow.Api.Errors;
using Voltflow.Api.Pagination;

namespace Voltflow.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder routes)
    {
        // Anyone signed in can look customers up; changing them is for managers.
        var group = routes.MapGroup("/api/customers").WithTags("02-QuoteToOrder");
        group.RequireAuthorization("Authenticated");

        group.MapGet("", async (string? search, string? type, bool? active, int? limit, int? offset, HttpContext httpContext, ICustomerService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(search, type, active, limit, offset, ct);
            if (result.IsSuccess) httpContext.ApplyPaginationHeaders(result.Value!);
            return result.From(page => Results.Ok(page.Items));
        })
        .WithName("VF-02101_ListCustomers");

        group.MapGet("{id:guid}", async (Guid id, ICustomerService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result.NotFound();
        })
        .WithName("VF-02101_GetCustomerById");

        group.MapPost("", async (CreateCustomerRequest request, ICustomerService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.From();
        })
        .WithName("VF-02101_CreateCustomer")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPut("{id:guid}", async (Guid id, UpdateCustomerRequest request, ICustomerService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-02102_UpdateCustomer")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("/{id:guid}/convert-to-active", async (Guid id, ICustomerService service, CancellationToken ct) =>
        {
            var result = await service.ConvertToActiveAsync(id, ct);
            return result.From();
        })
        .WithName("VF-02101_ConvertToActive")
        .RequireAuthorization("WriteAccess")
        .UseExecutionPolicy(ExecutionPolicy.OnceEver);

        group.MapPost("/{id:guid}/activate", async (Guid id, ICustomerService service, CancellationToken ct) =>
        {
            var result = await service.SetActiveAsync(id, true, ct);
            return result.From();
        })
        .WithName("VF-02103_ActivateCustomer")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("/{id:guid}/deactivate", async (Guid id, ICustomerService service, CancellationToken ct) =>
        {
            var result = await service.SetActiveAsync(id, false, ct);
            return result.From();
        })
        .WithName("VF-02104_DeactivateCustomer")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        // ---- addresses and the devices installed at them ----------------------------------------------------

        group.MapGet("/{id:guid}/sites", async (Guid id, ICustomerSiteService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(id, ct);
            return result.NotFound();
        })
        .WithName("VF-02111_ListCustomerSites");

        group.MapPost("/{id:guid}/sites", async (Guid id, SaveSiteRequest request, ICustomerSiteService service, CancellationToken ct) =>
        {
            var result = await service.CreateSiteAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-02112_CreateCustomerSite")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPut("/{id:guid}/sites/{siteId:guid}", async (Guid id, Guid siteId, SaveSiteRequest request, ICustomerSiteService service, CancellationToken ct) =>
        {
            var result = await service.UpdateSiteAsync(id, siteId, request, ct);
            return result.From();
        })
        .WithName("VF-02113_UpdateCustomerSite")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("/{id:guid}/sites/{siteId:guid}/assets", async (Guid id, Guid siteId, SaveAssetRequest request, ICustomerSiteService service, CancellationToken ct) =>
        {
            var result = await service.CreateAssetAsync(id, siteId, request, ct);
            return result.From();
        })
        .WithName("VF-02114_CreateCustomerAsset")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPut("/{id:guid}/sites/{siteId:guid}/assets/{assetId:guid}", async (Guid id, Guid siteId, Guid assetId, SaveAssetRequest request, ICustomerSiteService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAssetAsync(id, siteId, assetId, request, ct);
            return result.From();
        })
        .WithName("VF-02115_UpdateCustomerAsset")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        return routes;
    }
}
