using Microsoft.AspNetCore.Http;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Api.Security;
using Voltflow.Api.Errors;

namespace Voltflow.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/customers").WithTags("02-QuoteToOrder");
        group.RequireAuthorization("Authenticated");

        group.MapGet("", async (ICustomerService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(ct);
            return result.From();
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

        group.MapPost("/{id:guid}/convert-to-active", async (Guid id, ICustomerService service, CancellationToken ct) =>
        {
            var result = await service.ConvertToActiveAsync(id, ct);
            return result.From();
        })
        .WithName("VF-02101_ConvertToActive")
        .RequireAuthorization("WriteAccess")
        .UseExecutionPolicy(ExecutionPolicy.OnceEver);

        return routes;
    }
}
