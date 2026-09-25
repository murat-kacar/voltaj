using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Shared;
using Voltflow.Api.Errors;

namespace Voltflow.Api.Endpoints;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/services")
            .WithTags("Services")
            .RequireAuthorization();

        group.MapGet("/", async ([FromServices] IServiceService services,
            string? search, string? status, Guid? customerId, Guid? assignedUserId,
            int? limit, int? offset, CancellationToken ct) =>
        {
            var page = await services.ListAsync(search, status, customerId, assignedUserId, limit, offset, ct);
            return ApiResults.From(page);
        });

        group.MapGet("/{id:guid}", async ([FromServices] IServiceService services,
            Guid id, CancellationToken ct) =>
        {
            var service = await services.GetByIdAsync(id, ct);
            return ApiResults.From(service);
        });

        // Draft phase
        group.MapPost("/", async ([FromServices] IServiceService services,
            [FromBody] CreateServiceDraftRequest request, CancellationToken ct) =>
        {
            var result = await services.CreateDraftAsync(request, ct);
            return ApiResults.From(result);
        });

        group.MapPut("/{id:guid}", async ([FromServices] IServiceService services,
            Guid id, [FromBody] UpdateServiceDraftRequest request, CancellationToken ct) =>
        {
            var result = await services.UpdateDraftAsync(id, request, ct);
            return ApiResults.From(result);
        });

        group.MapPost("/{id:guid}/items", async ([FromServices] IServiceService services,
            Guid id, [FromBody] ServiceLineRequest request, CancellationToken ct) =>
        {
            var result = await services.AddItemAsync(id, request, ct);
            return ApiResults.From(result);
        });

        group.MapDelete("/{id:guid}/items/{itemId:guid}", async ([FromServices] IServiceService services,
            Guid id, Guid itemId, [FromQuery] string? auditNote, CancellationToken ct) =>
        {
            var result = await services.RemoveItemAsync(id, itemId, auditNote ?? string.Empty, ct);
            return ApiResults.From(result);
        });

        group.MapPost("/{id:guid}/issue", async ([FromServices] IServiceService services,
            Guid id, CancellationToken ct) =>
        {
            var result = await services.IssueAsync(id, ct);
            return ApiResults.From(result);
        });

        // Acceptance & Active Phase
        group.MapPost("/{id:guid}/accept", async ([FromServices] IServiceService services,
            Guid id, [FromBody] AcceptServiceRequest request, CancellationToken ct) =>
        {
            var result = await services.AcceptAsync(id, request, ct);
            return ApiResults.From(result);
        });

        group.MapPost("/{id:guid}/reject", async ([FromServices] IServiceService services,
            Guid id, [FromBody] RejectServiceRequest request, CancellationToken ct) =>
        {
            var result = await services.RejectAsync(id, request.Reason, ct);
            return ApiResults.From(result);
        });

        group.MapPost("/{id:guid}/deposit", async ([FromServices] IServiceService services,
            Guid id, [FromBody] PayServiceDepositRequest request, CancellationToken ct) =>
        {
            var result = await services.PayDepositAsync(id, request, ct);
            return ApiResults.From(result);
        });
        
        group.MapPost("/{id:guid}/cancel", async ([FromServices] IServiceService services,
            Guid id, [FromBody] CancelServiceRequest request, CancellationToken ct) =>
        {
            var result = await services.CancelAsync(id, request.Reason, ct);
            return ApiResults.From(result);
        });

        // Operations
        group.MapPost("/{id:guid}/assign", async ([FromServices] IServiceService services,
            Guid id, [FromBody] AssignServiceRequest request, CancellationToken ct) =>
        {
            var result = await services.AssignAsync(id, request, ct);
            return ApiResults.From(result);
        });

        group.MapPost("/{id:guid}/hold", async ([FromServices] IServiceService services,
            Guid id, [FromBody] HoldServiceRequest request, CancellationToken ct) =>
        {
            var result = await services.HoldAsync(id, request, ct);
            return ApiResults.From(result);
        });

        group.MapPost("/{id:guid}/resume", async ([FromServices] IServiceService services,
            Guid id, CancellationToken ct) =>
        {
            var result = await services.ResumeAsync(id, ct);
            return ApiResults.From(result);
        });

        group.MapPost("/{id:guid}/complete", async ([FromServices] IServiceService services,
            Guid id, [FromBody] CompleteServiceRequest request, CancellationToken ct) =>
        {
            var result = await services.CompleteAsync(id, request, ct);
            return ApiResults.From(result);
        });

        // Billing
        group.MapPost("/{id:guid}/partial-invoice", async ([FromServices] IServiceService services,
            Guid id, [FromBody] IssuePartialInvoiceRequest request, CancellationToken ct) =>
        {
            var result = await services.IssuePartialInvoiceAsync(id, request, ct);
            return ApiResults.From(result);
        });

        group.MapPost("/{id:guid}/substatus", async ([FromServices] IServiceService services,
            Guid id, [FromBody] UpdateSubStatusRequest request, CancellationToken ct) =>
        {
            var result = await services.UpdateSubStatusAsync(id, request, ct);
            return ApiResults.From(result);
        });
    }
}
