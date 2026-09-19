using Voltflow.Api.Errors;
using Voltflow.Api.Pagination;
using Voltflow.Api.Security;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;

namespace Voltflow.Api.Endpoints;

public static class CashShiftEndpoints
{
    public static IEndpointRouteBuilder MapCashShiftEndpoints(this IEndpointRouteBuilder routes)
    {
        var shifts = routes.MapGroup("/api/cash-shifts").WithTags("09-QuickSale");
        shifts.RequireAuthorization("OperationsAccess");

        // 204 when the user has no open shift, so the screen can tell "none" from a failure.
        shifts.MapGet("current", async (ICashShiftService service, CancellationToken ct) =>
        {
            var result = await service.GetCurrentAsync(ct);
            return result.From(report => report is null ? Results.NoContent() : Results.Ok(report));
        })
        .WithName("VF-09201_GetCurrentCashShift");

        shifts.MapPost("open", async (OpenShiftRequest request, ICashShiftService service, CancellationToken ct) =>
        {
            var result = await service.OpenAsync(request, ct);
            return result.From();
        })
        .WithName("VF-09202_OpenCashShift")
        .UseMutationPolicy();

        shifts.MapPost("{id:guid}/close", async (Guid id, CloseShiftRequest request, ICashShiftService service, CancellationToken ct) =>
        {
            var result = await service.CloseAsync(id, request, ct);
            return result.From();
        })
        .WithName("VF-09203_CloseCashShift")
        .UseMutationPolicy();

        shifts.MapGet("", async (int? limit, int? offset, HttpContext httpContext, ICashShiftService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(limit, offset, ct);
            if (result.IsSuccess) httpContext.ApplyPaginationHeaders(result.Value!);
            return result.From(page => Results.Ok(page.Items));
        })
        .WithName("VF-09204_ListCashShifts");

        shifts.MapGet("{id:guid}/report", async (Guid id, ICashShiftService service, CancellationToken ct) =>
        {
            var result = await service.GetReportAsync(id, ct);
            return result.NotFound();
        })
        .WithName("VF-09205_GetCashShiftReport");

        return routes;
    }
}
