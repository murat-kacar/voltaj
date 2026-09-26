using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Sales;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class QuickSaleService : IQuickSaleService
{
    private readonly IQuickSaleRepository _repository;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IDocumentNumbers _documentNumbers;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public QuickSaleService(
        IQuickSaleRepository repository,
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IDocumentNumbers documentNumbers,
        ICommandJournal commandJournal,
        IOperationContext operationContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _documentNumbers = documentNumbers;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<QuickSaleDto>> CompleteSaleAsync(StartQuickSaleRequest request, CancellationToken ct = default)
    {
        if (request.Lines == null || request.Lines.Count == 0)
            return Result<QuickSaleDto>.Fail("A quick sale must have at least one line item.");
        if (request.Payments == null || request.Payments.Count == 0)
            return Result<QuickSaleDto>.Fail("A quick sale must have at least one payment method provided.");

        var userId = _currentUser.UserId ?? Guid.Empty;
        var userName = "Cashier";
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var nextNumberFunc = await _documentNumbers.PrepareAsync("QS", "QS-", ct);
        try
        {
            var sale = QuickSale.Create(
                nextNumberFunc,
                now,
                userId,
                userName,
                request.ShiftId == Guid.Empty ? Guid.NewGuid() : request.ShiftId,
                request.CustomerId,
                request.Lines,
                request.ReceiptDiscount,
                request.Payments,
                request.Note
            );

            await _repository.AddAsync(sale, ct);

            // Trigger Policies (Events)
            // Deduct Inventory
            foreach (var line in request.Lines.Where(l => l.TracksStock && l.ProductId.HasValue))
            {
                // Here we call the Inventory service to deduct stock
                await _inventoryService.ApplyMovementAsync(
                    line.ProductCode ?? "",
                    line.Quantity,
                    Voltflow.Domain.Inventory.StockMovementType.Out,
                    $"Sale #{sale.SaleNumber}",
                    ct
                );
            }

            // Record Finance
            if (request.CustomerId.HasValue)
            {
                foreach (var payment in request.Payments)
                {
                    await _paymentService.CreateAsync(new CreatePaymentRequest(
                        request.CustomerId.Value,
                        payment.Amount,
                        payment.Method.ToString(),
                        DateOnly.FromDateTime(now)
                    ), ct);
                }
            }

            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: true, errorCode: null, ct);
            return Result<QuickSaleDto>.Ok(Map(sale));
        }
        catch (InvalidOperationException ex)
        {
            return Result<QuickSaleDto>.Fail(ex.Message);
        }
    }

    public async Task<Result<QuickSaleDto>> VoidSaleAsync(Guid id, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result<QuickSaleDto>.Fail("Void reason is required.");

        var sale = await _repository.GetByIdAsync(id, ct);
        if (sale is null) return Result<QuickSaleDto>.Fail("Sale not found.");

        try
        {
            sale.Void(_currentUser.UserId ?? Guid.Empty, reason, _timeProvider.GetUtcNow().UtcDateTime);
            await _repository.UpdateAsync(sale, ct);

            // We would also need to revert inventory and payments here (Saga/Compensation H10/H11)
            // Simplified for now.

            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: true, errorCode: null, ct);
            return Result<QuickSaleDto>.Ok(Map(sale));
        }
        catch (InvalidOperationException ex)
        {
            return Result<QuickSaleDto>.Fail(ex.Message);
        }
    }

    public async Task<Result<QuickSaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await _repository.GetByIdAsync(id, ct);
        if (sale is null) return Result<QuickSaleDto>.Fail("Sale not found.");

        return Result<QuickSaleDto>.Ok(Map(sale));
    }

    public async Task<Result<PagedResult<QuickSaleSummaryDto>>> ListAsync(int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var page = await _repository.ListPagedAsync(
            PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        
        return Result<PagedResult<QuickSaleSummaryDto>>.Ok(page.Map(MapSummary));
    }

    private static QuickSaleDto Map(QuickSale s) => new(
        s.Id,
        s.SaleNumber,
        s.SoldAt,
        s.CashierUserId,
        s.CashierName,
        s.ShiftId,
        s.CustomerId,
        s.Status.ToString(),
        s.Subtotal,
        s.LineDiscountTotal,
        s.ReceiptDiscount,
        s.GrandTotal,
        s.VatTotal,
        s.CashTendered,
        s.ChangeGiven,
        s.Note,
        s.Lines.Select(MapLine).ToList(),
        s.Payments.Select(MapPayment).ToList()
    );

    private static QuickSaleLineDto MapLine(QuickSaleLine l) => new(
        l.ProductId,
        l.ProductCode,
        l.Barcode,
        l.Description,
        l.Unit,
        l.Quantity,
        l.UnitPrice,
        l.VatRate,
        l.LineDiscount,
        l.LineTotal
    );

    private static QuickSalePaymentDto MapPayment(QuickSalePayment p) => new(
        p.Method,
        p.Amount,
        p.Reference
    );

    private static QuickSaleSummaryDto MapSummary(QuickSale s) => new(
        s.Id,
        s.SaleNumber,
        s.SoldAt,
        s.CashierName,
        s.CustomerId,
        s.Status.ToString(),
        s.GrandTotal
    );
}
