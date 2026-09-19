using Microsoft.AspNetCore.Http;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Api.Security;
using Voltflow.Api.Errors;
using Voltflow.Api.Pagination;

namespace Voltflow.Api.Endpoints;

public static class QuoteEndpoints
{
    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/quotes").WithTags("02-QuoteToOrder");
        group.RequireAuthorization("Authenticated");

        // Anyone signed in can read quotes; changing them is for managers.
        group.MapGet("", async (string? search, string? state, Guid? customerId, int? limit, int? offset, HttpContext httpContext, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(search, state, customerId, limit, offset, ct);
            if (result.IsSuccess) httpContext.ApplyPaginationHeaders(result.Value!);
            return result.From(page => Results.Ok(page.Items));
        })
        .WithName("VF-02301_ListQuotes");

        group.MapGet("{id:guid}", async (Guid id, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result.NotFound();
        })
        .WithName("VF-02301_GetQuoteById");

        group.MapPost("", async (CreateQuoteRequest request, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.From();
        })
        .WithName("VF-02301_CreateQuoteDraft")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPut("{id:guid}", async (Guid id, UpdateQuoteRequest request, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-02302_UpdateQuoteDraft")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapDelete("{id:guid}", async (Guid id, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            return result.From();
        })
        .WithName("VF-02304_DeleteQuoteDraft")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("{id:guid}/copy", async (Guid id, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.CopyAsync(id, ct);
            return result.From();
        })
        .WithName("VF-02303_CopyQuote")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("{id:guid}/items", async (Guid id, QuoteLineRequest request, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.AddItemAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-02301_AddQuoteItem")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("{id:guid}/issue", async (Guid id, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.IssueAsync(id, ct);
            return result.From();
        })
        .WithName("VF-02301_IssueQuote")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("{id:guid}/accept", async (Guid id, AcceptQuoteRequest request, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.AcceptAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-02401_AcceptQuote")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("{id:guid}/pay-deposit", async (Guid id, PayQuoteDepositRequest request, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.PayDepositAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-02401_PayQuoteDeposit")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("{id:guid}/reject", async (Guid id, RejectQuoteRequest request, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.RejectAsync(id, request.Reason, ct);
            return result.From();
        })
        .WithName("VF-02401_RejectQuote")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("{id:guid}/expire", async (Guid id, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.ExpireAsync(id, ct);
            return result.From();
        })
        .WithName("VF-02401_ExpireQuote")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        group.MapPost("{id:guid}/work-order", async (Guid id, IQuoteService service, CancellationToken ct) =>
        {
            var result = await service.ConvertAcceptedToWorkOrderAsync(id, ct);
            return result.From();
        })
        .WithName("VF-02501_ConvertToWorkOrder")
        .RequireAuthorization("WriteAccess")
        .UseExecutionPolicy(ExecutionPolicy.OnceEver);

        return routes;
    }
}
