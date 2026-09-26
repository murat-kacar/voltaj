using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Voltflow.Api.Errors;
using Voltflow.Api.Pagination;
using Voltflow.Api.Security;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Endpoints;

public static class ReminderEndpoints
{
    public static IEndpointRouteBuilder MapReminderEndpoints(this IEndpointRouteBuilder routes)
    {
        var reminders = routes.MapGroup("/api/reminders").WithTags("07-Reminders");
        reminders.RequireAuthorization("Authenticated");

        reminders.MapGet("", async (string? state, int? limit, int? offset, HttpContext httpContext, IReminderService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(state, limit, offset, ct);
            if (result.IsSuccess) httpContext.ApplyPaginationHeaders(result.Value!);
            return result.From(page => Results.Ok(page.Items));
        })
        .WithName("VF-07101_ListReminders");

        reminders.MapPost("", async (CreateReminderRequest request, IReminderService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.From();
        })
        .WithName("VF-07101_CreateReminder")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        reminders.MapPost("{id:guid}/dismiss", async (Guid id, DismissReminderRequest request, IReminderService service, CancellationToken ct) =>
        {
            var result = await service.DismissAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-07101_DismissReminder")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        reminders.MapPost("{id:guid}/complete", async (Guid id, CompleteReminderRequest request, IReminderService service, CancellationToken ct) =>
        {
            var result = await service.CompleteAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-07101_CompleteReminder")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        return routes;
    }
}
