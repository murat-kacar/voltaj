using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Voltflow.Api.Errors;
using Voltflow.Api.Pagination;
using Voltflow.Api.Security;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Endpoints;

public static class WorkOrderEndpoints
{
    public static IEndpointRouteBuilder MapWorkOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        var workOrders = routes.MapGroup("/api/workorders").WithTags("03-WorkOrderExecution");
        workOrders.RequireAuthorization("Authenticated");

        workOrders.MapGet("", async (int? limit, int? offset, HttpContext httpContext, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(limit, offset, ct);
            if (result.IsSuccess) httpContext.ApplyPaginationHeaders(result.Value!);
            return result.From(page => Results.Ok(page.Items));
        })
        .WithName("VF-03101_ListWorkOrders")
        .RequireAuthorization("OperationsAccess");

        workOrders.MapGet("{id:guid}", async (Guid id, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result.NotFound();
        })
        .WithName("VF-03101_GetWorkOrderById")
        .RequireAuthorization("OperationsAccess");

        workOrders.MapPost("", async (CreateWorkOrderRequest request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.From();
        })
        .WithName("VF-03101_CreateWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/assign", async (Guid id, AssignWorkOrderRequest request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.AssignAsync(id, request.EmployeeUserId, ct);
            return result.From();
        })
        .WithName("VF-03101_AssignWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/en-route", async (Guid id, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.MarkAsEnRouteAsync(id, ct);
            return result.From();
        })
        .WithName("VF-03101_MarkEnRoute")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/no-show", async (Guid id, ReportNoShowRequest request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.ReportNoShowAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-03101_ReportNoShow")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/safety-checklist", async (Guid id, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.CompleteSafetyChecklistAsync(id, ct);
            return result.From();
        })
        .WithName("VF-03101_CompleteSafetyChecklist")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/start", async (Guid id, StartWorkOrderRequest request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.StartAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-03101_StartWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/check-in", async (Guid id, CheckInWorkOrderRequest? request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.CheckInAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-03201_CheckInWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/check-out", async (Guid id, CheckOutWorkOrderRequest? request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.CheckOutAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-03201_CheckOutWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/hold", async (Guid id, HoldWorkOrderRequest request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.PutOnHoldAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-03301_HoldWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/resume", async (Guid id, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.ResumeAsync(id, ct);
            return result.From();
        })
        .WithName("VF-03301_ResumeWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/cancel", async (Guid id, CancelWorkOrderRequest request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.CancelAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-03501_CancelWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/complete", async (Guid id, CompleteWorkOrderRequest request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.CompleteAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-03401_CompleteWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/approve-billing", async (Guid id, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.ApproveForBillingAsync(id, ct);
            return result.From();
        })
        .WithName("VF-03501_ApproveBilling")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/invoice", async (Guid id, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.InvoiceAsync(id, ct);
            return result.From();
        })
        .WithName("VF-03501_InvoiceWorkOrder")
        .RequireAuthorization("WriteAccess")
        .UseMutationPolicy();

        workOrders.MapPost("{id:guid}/items", async (Guid id, AddMaterialToWorkOrderRequest request, IWorkOrderService service, CancellationToken ct) =>
        {
            var result = await service.AddMaterialAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-03401_AddMaterialToWorkOrder")
        .RequireAuthorization("OperationsAccess")
        .UseMutationPolicy();

        return routes;
    }
}
