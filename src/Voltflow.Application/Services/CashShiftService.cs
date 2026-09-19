using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Sales;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class CashShiftService : ICashShiftService
{
    private const decimal MaxAmount = 100_000_000m;
    private const int MaxNoteLength = 500;

    private readonly ISalesRepository _sales;
    private readonly IAppUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public CashShiftService(
        ISalesRepository sales,
        IAppUserRepository users,
        ICurrentUser currentUser,
        ICommandJournal commandJournal,
        IOperationContext operationContext,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _sales = sales;
        _users = users;
        _currentUser = currentUser;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CashShiftReportDto?>> GetCurrentAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId) return Result<CashShiftReportDto?>.Fail("Sign in first.", "UNAUTHENTICATED");

        var shift = await _sales.GetOpenShiftAsync(userId, ct);
        return Result<CashShiftReportDto?>.Ok(shift is null ? null : await BuildReportAsync(shift, ct));
    }

    public async Task<Result<CashShiftReportDto>> OpenAsync(OpenShiftRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId) return Result<CashShiftReportDto>.Fail("Sign in first.", "UNAUTHENTICATED");
        if (request.OpeningCash < 0 || request.OpeningCash > MaxAmount)
            return Result<CashShiftReportDto>.Fail($"Opening cash must be between 0 and {MaxAmount:N0}.");
        if (await _sales.GetOpenShiftAsync(userId, ct) is not null)
            return Result<CashShiftReportDto>.Fail("You already have an open shift.", "SHIFT_ALREADY_OPEN");

        var user = await _users.GetByIdAsync(userId, ct);
        var shift = CashShift.Open(userId, user?.Name ?? "Unknown", request.OpeningCash, _clock.GetUtcNow().UtcDateTime);
        _sales.AddShift(shift);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);
        return Result<CashShiftReportDto>.Ok(await BuildReportAsync(shift, ct));
    }

    public async Task<Result<CashShiftReportDto>> CloseAsync(Guid id, CloseShiftRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId) return Result<CashShiftReportDto>.Fail("Sign in first.", "UNAUTHENTICATED");
        if (request.CountedCash < 0 || request.CountedCash > MaxAmount)
            return Result<CashShiftReportDto>.Fail($"The counted cash must be between 0 and {MaxAmount:N0}.");
        if (request.Note is { Length: > MaxNoteLength })
            return Result<CashShiftReportDto>.Fail($"The note can be at most {MaxNoteLength} characters.");

        var shift = await FindVisibleShiftAsync(id, ct);
        if (shift is null) return Result<CashShiftReportDto>.Fail("Shift not found.", "SHIFT_NOT_FOUND");
        if (shift.Status != CashShiftStatus.Open) return Result<CashShiftReportDto>.Fail("The shift is already closed.", "SHIFT_CLOSED");

        // What the drawer should hold is worked out from the sales and returns of the shift, never taken from the client.
        var totals = await _sales.GetShiftTotalsAsync(shift.Id, ct);
        shift.Close(userId, request.CountedCash, ExpectedCash(shift, totals), request.Note, _clock.GetUtcNow().UtcDateTime);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);
        return Result<CashShiftReportDto>.Ok(Build(shift, totals));
    }

    public async Task<Result<CashShiftReportDto>> GetReportAsync(Guid id, CancellationToken ct = default)
    {
        var shift = await FindVisibleShiftAsync(id, ct);
        return shift is null
            ? Result<CashShiftReportDto>.Fail("Shift not found.", "SHIFT_NOT_FOUND")
            : Result<CashShiftReportDto>.Ok(await BuildReportAsync(shift, ct));
    }

    public async Task<Result<PagedResult<CashShiftDto>>> ListAsync(int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var page = await _sales.ListShiftsPagedAsync(
            SeesEveryCashier ? null : _currentUser.UserId,
            PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        return Result<PagedResult<CashShiftDto>>.Ok(page.Map(Map));
    }

    /// <summary>Managers and administrators see every shift; anyone else sees only their own.</summary>
    private bool SeesEveryCashier => _currentUser.Roles.Any(role => role is "Admin" or "Manager");

    private async Task<CashShift?> FindVisibleShiftAsync(Guid id, CancellationToken ct)
    {
        var shift = await _sales.GetShiftAsync(id, ct);
        if (shift is not null && !SeesEveryCashier && shift.CashierUserId != _currentUser.UserId) return null;
        return shift;
    }

    private async Task<CashShiftReportDto> BuildReportAsync(CashShift shift, CancellationToken ct)
        => Build(shift, await _sales.GetShiftTotalsAsync(shift.Id, ct));

    /// <summary>The float, plus the cash that was paid in, minus the cash that was paid back out.</summary>
    private static decimal ExpectedCash(CashShift shift, ShiftTotals totals)
        => shift.OpeningCash + totals.PaidBy(SalePaymentMethod.Cash) - totals.CashRefunds;

    private static CashShiftDto Map(CashShift shift) => new(
        shift.Id, shift.CashierUserId, shift.CashierName, shift.OpenedAt, shift.OpeningCash, shift.Status.ToString(),
        shift.ClosedAt, shift.CountedCash, shift.ExpectedCash, shift.CashDifference, shift.Note);

    private static CashShiftReportDto Build(CashShift shift, ShiftTotals totals) => new(
        Map(shift),
        totals.SaleCount,
        totals.VoidedCount,
        totals.SalesTotal,
        totals.DiscountTotal,
        totals.VatTotal,
        totals.PaidBy(SalePaymentMethod.Cash),
        totals.PaidBy(SalePaymentMethod.Card),
        totals.PaidBy(SalePaymentMethod.BankTransfer),
        totals.ReturnCount,
        totals.ReturnTotal,
        totals.CashRefunds,
        totals.SalesTotal - totals.ReturnTotal,
        ExpectedCash(shift, totals),
        totals.VatBuckets.Select(bucket => new VatBucketDto(bucket.Rate, bucket.Gross, bucket.Vat)).ToList());
}
