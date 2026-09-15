using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Api.Security;
using Voltflow.Api.Errors;

namespace Voltflow.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/payments").WithTags("05-Finance");
        group.RequireAuthorization("Authenticated");

        group.MapGet("{customerId:guid}", async (Guid customerId, IPaymentService service, CancellationToken ct) =>
        {
            var result = await service.ListByCustomerAsync(customerId, ct);
            return result.From();
        })
        .WithName("VF-05201_ListPaymentsByCustomer");

        group.MapGet("invoices/{customerId:guid}", async (Guid customerId, IPaymentService service, CancellationToken ct) =>
        {
            var result = await service.ListInvoicesByCustomerAsync(customerId, ct);
            return result.From();
        })
        .WithName("VF-05101_ListInvoicesByCustomer");

        group.MapPost("", async (CreatePaymentRequest request, IPaymentService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.From();
        })
        .WithName("VF-05201_CreatePayment")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("/allocate", async (AllocatePaymentRequest request, IPaymentService service, CancellationToken ct) =>
        {
            var result = await service.AllocateToInvoiceAsync(request, ct);
            return result.From();
        })
        .WithName("VF-05301_AllocatePaymentToInvoice")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        return routes;
    }
}