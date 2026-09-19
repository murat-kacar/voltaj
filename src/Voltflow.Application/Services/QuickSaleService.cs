using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Inventory;
using Voltflow.Domain.Sales;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

/// <summary>
/// The counter. Every operation follows one order: check everything (reads only), then change state, then commit once.
/// The database context is shared with the command journal, which saves whatever is tracked when a request fails, so
/// a rule may never be checked after something has already been changed.
/// </summary>
public sealed class QuickSaleService : IQuickSaleService
{
    private const string SaleCounterKey = "quick-sale";
    private const string ReturnCounterKey = "quick-sale-return";
    private const int MaxLines = 100;
    private const int MaxTextLength = 500;
    private const int MaxDescriptionLength = 200;
    private const decimal MaxQuantity = 100_000m;
    private const decimal MaxAmount = 100_000_000m;

    private readonly ISalesRepository _sales;
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly ICustomerRepository _customers;
    private readonly IAppUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentNumbers _numbers;
    private readonly TimeProvider _clock;

    public QuickSaleService(
        ISalesRepository sales,
        IProductRepository products,
        IInventoryRepository inventory,
        ICustomerRepository customers,
        IAppUserRepository users,
        ICurrentUser currentUser,
        ICommandJournal commandJournal,
        IOperationContext operationContext,
        IUnitOfWork unitOfWork,
        IDocumentNumbers numbers,
        TimeProvider clock)
    {
        _sales = sales;
        _products = products;
        _inventory = inventory;
        _customers = customers;
        _users = users;
        _currentUser = currentUser;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
        _unitOfWork = unitOfWork;
        _numbers = numbers;
        _clock = clock;
    }

    // ---- sell --------------------------------------------------------------------------------------------

    public async Task<Result<QuickSaleDto>> CreateAsync(CreateQuickSaleRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId) return Fail("Sign in first.", "UNAUTHENTICATED");
        if (request.Lines is null || request.Lines.Count == 0) return Fail("A sale needs at least one line.");
        if (request.Lines.Count > MaxLines) return Fail($"A sale can have at most {MaxLines} lines.");
        if (request.Payments is null || request.Payments.Count == 0) return Fail("At least one payment is required.");
        if (request.Note is { Length: > MaxTextLength }) return Fail($"The note can be at most {MaxTextLength} characters.");
        if (request.ReceiptDiscount < 0 || request.ReceiptDiscount > MaxAmount) return Fail("The receipt discount is out of range.");

        var shift = await _sales.GetOpenShiftAsync(userId, ct);
        if (shift is null) return Fail("Open a shift before selling.", "SHIFT_REQUIRED");

        if (request.CustomerId is { } customerId && await _customers.GetByIdAsync(customerId, ct) is null)
            return Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

        var productIds = request.Lines.Where(line => line.ProductId.HasValue).Select(line => line.ProductId!.Value).Distinct().ToList();
        var products = (await _products.GetByIdsAsync(productIds, ct)).ToDictionary(product => product.Id);

        var inputs = new List<QuickSaleLineInput>(request.Lines.Count);
        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0 || line.Quantity > MaxQuantity || decimal.Round(line.Quantity, 2) != line.Quantity)
                return Fail($"The quantity must be between 0.01 and {MaxQuantity:N0}, with at most two decimals.");
            if (line.DiscountAmount < 0 || line.DiscountAmount > MaxAmount) return Fail("The discount is out of range.");

            if (line.ProductId is { } productId)
            {
                if (!products.TryGetValue(productId, out var product)) return Fail("Product not found.", "PRODUCT_NOT_FOUND");
                if (!product.IsActive) return Fail($"'{product.Name}' is not for sale.", "PRODUCT_INACTIVE");
                inputs.Add(new QuickSaleLineInput(
                    product.Id, product.Code, product.Barcode, product.Name, product.Unit,
                    line.Quantity, product.SalePrice, product.VatRate, line.DiscountAmount, product.TracksStock));
                continue;
            }

            // a free line: not on the price list, so the cashier gives the description, price and VAT and no stock moves
            if (string.IsNullOrWhiteSpace(line.Description)) return Fail("A line without a product needs a description.");
            if (line.Description.Trim().Length > MaxDescriptionLength) return Fail($"A description can be at most {MaxDescriptionLength} characters.");
            if (line.UnitPrice is not { } price || price < 0 || price > MaxAmount) return Fail("A line without a product needs a unit price.");
            if (line.VatRate is not { } vatRate || vatRate < 0 || vatRate > 100) return Fail("A line without a product needs a VAT rate between 0 and 100.");
            inputs.Add(new QuickSaleLineInput(null, null, null, line.Description.Trim(), "adet", line.Quantity, price, vatRate, line.DiscountAmount, false));
        }

        var payments = new List<QuickSalePaymentInput>(request.Payments.Count);
        foreach (var payment in request.Payments)
        {
            if (!TryParseMethod(payment.Method, out var method)) return Fail("Unknown payment method.");
            if (payment.Amount <= 0 || payment.Amount > MaxAmount) return Fail("Every payment must be greater than zero.");
            if (payment.Reference is { Length: > MaxDescriptionLength }) return Fail($"A payment reference can be at most {MaxDescriptionLength} characters.");
            payments.Add(new QuickSalePaymentInput(method, payment.Amount, payment.Reference));
        }

        // stock: every product that tracks it must have enough, counted over all its lines
        var tracked = inputs
            .Where(input => input.TracksStock)
            .GroupBy(input => input.ProductCode!)
            .Select(group => (Code: group.Key, Quantity: group.Sum(input => input.Quantity)))
            .ToList();
        var stocks = await LoadStockAsync(tracked.Select(item => item.Code), ct);
        foreach (var (code, quantity) in tracked)
        {
            var available = stocks.TryGetValue(code, out var stock) ? stock.AvailableQuantity : 0m;
            if (quantity > available) return Fail($"Not enough stock for '{code}': {available:0.##} available.", "INSUFFICIENT_STOCK");
        }

        var cashier = await _users.GetByIdAsync(userId, ct);
        var nextNumber = await _numbers.PrepareAsync(SaleCounterKey, "HS", ct);

        // From here the sale is built and things change; the domain still checks the discounts and the payments, and
        // it takes a receipt number only once they pass.
        QuickSale sale;
        try
        {
            sale = QuickSale.Create(
                nextNumber, _clock.GetUtcNow().UtcDateTime, userId, cashier?.Name ?? "Unknown", shift.Id,
                request.CustomerId, inputs, request.ReceiptDiscount, payments, request.Note);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Fail(exception.Message, "SALE_REJECTED");
        }

        foreach (var (code, quantity) in tracked)
            _inventory.ApplyMovement(stocks[code], -quantity, StockMovementType.Out, $"Quick sale {sale.SaleNumber}");
        _sales.AddSale(sale);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);

        return Result<QuickSaleDto>.Ok(await MapAsync(sale, ct));
    }

    // ---- look up -----------------------------------------------------------------------------------------

    public async Task<Result<QuickSaleDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await _sales.GetSaleAsync(id, ct);
        if (sale is null || !CanSee(sale)) return Fail("Sale not found.", "SALE_NOT_FOUND");
        return Result<QuickSaleDto>.Ok(await MapAsync(sale, ct));
    }

    public async Task<Result<PagedResult<QuickSaleSummaryDto>>> ListAsync(
        string? search, string? status, DateTime? from, DateTime? to, Guid? customerId = null, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        QuickSaleStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<QuickSaleStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                return Result<PagedResult<QuickSaleSummaryDto>>.Fail("Unknown status.");
            statusFilter = parsed;
        }

        var filter = new QuickSaleFilter(search, statusFilter, from, to, SeesEveryCashier ? null : _currentUser.UserId, customerId);
        var page = await _sales.ListSalesPagedAsync(filter, PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        return Result<PagedResult<QuickSaleSummaryDto>>.Ok(page.Map(MapSummary));
    }

    // ---- void and return ---------------------------------------------------------------------------------

    public async Task<Result<QuickSaleDto>> VoidAsync(Guid id, VoidQuickSaleRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId) return Fail("Sign in first.", "UNAUTHENTICATED");
        if (string.IsNullOrWhiteSpace(request.Reason)) return Fail("A reason is required.");
        if (request.Reason.Trim().Length > MaxTextLength) return Fail($"The reason can be at most {MaxTextLength} characters.");

        var sale = await _sales.GetSaleAsync(id, ct);
        if (sale is null) return Fail("Sale not found.", "SALE_NOT_FOUND");
        if (sale.Status != QuickSaleStatus.Completed) return Fail("Only a completed sale can be voided.", "SALE_NOT_COMPLETED");
        if (sale.Lines.Any(line => line.ReturnedQuantity > 0)) return Fail("A sale that has returns cannot be voided.", "SALE_HAS_RETURNS");

        // Closed shifts are final. Once the drawer was counted, goods come back through a return, not by rewriting the sale.
        var shift = await _sales.GetShiftAsync(sale.ShiftId, ct);
        if (shift is null || shift.Status != CashShiftStatus.Open)
            return Fail("Only a sale of a shift that is still open can be voided; take the goods back with a return instead.", "SHIFT_CLOSED");

        var stockLines = sale.Lines.Where(line => line.TracksStock && line.ProductCode is not null).ToList();
        var stockCodes = stockLines.Select(line => line.ProductCode!).Distinct().ToList();
        var stocks = await LoadStockAsync(stockCodes, ct);
        var missing = stockCodes.FirstOrDefault(code => !stocks.ContainsKey(code));
        if (missing is not null) return Fail($"The stock record of '{missing}' is missing.", "STOCK_RECORD_MISSING");

        try
        {
            sale.Void(userId, request.Reason, _clock.GetUtcNow().UtcDateTime);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Fail(exception.Message, "VOID_REJECTED");
        }

        foreach (var group in stockLines.GroupBy(line => line.ProductCode!))
            _inventory.ApplyMovement(stocks[group.Key], group.Sum(line => line.Quantity), StockMovementType.Return, $"Void of sale {sale.SaleNumber}");
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);

        return Result<QuickSaleDto>.Ok(await MapAsync(sale, ct));
    }

    public async Task<Result<QuickSaleDto>> ReturnAsync(Guid id, ReturnQuickSaleRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId) return Fail("Sign in first.", "UNAUTHENTICATED");
        if (string.IsNullOrWhiteSpace(request.Reason)) return Fail("A reason is required.");
        if (request.Reason.Trim().Length > MaxTextLength) return Fail($"The reason can be at most {MaxTextLength} characters.");
        if (!TryParseMethod(string.IsNullOrWhiteSpace(request.RefundMethod) ? nameof(SalePaymentMethod.Cash) : request.RefundMethod, out var refundMethod))
            return Fail("Unknown refund method.");
        if (request.Items is null || request.Items.Count == 0) return Fail("Choose at least one line to return.");
        if (request.Items.Any(item => item.Quantity <= 0 || item.Quantity > MaxQuantity || decimal.Round(item.Quantity, 2) != item.Quantity))
            return Fail("Every returned quantity must be greater than zero, with at most two decimals.");

        var sale = await _sales.GetSaleAsync(id, ct);
        if (sale is null) return Fail("Sale not found.", "SALE_NOT_FOUND");

        // The refund leaves the till of whoever takes the goods back, so that person needs an open shift.
        var shift = await _sales.GetOpenShiftAsync(userId, ct);
        if (shift is null) return Fail("Open a shift before taking a return.", "SHIFT_REQUIRED");

        var requested = request.Items.Select(item => item.LineId).ToHashSet();
        var stockCodes = sale.Lines
            .Where(line => requested.Contains(line.Id) && line.TracksStock && line.ProductCode is not null)
            .Select(line => line.ProductCode!)
            .Distinct()
            .ToList();
        var stocks = await LoadStockAsync(stockCodes, ct);
        var missing = stockCodes.FirstOrDefault(code => !stocks.ContainsKey(code));
        if (missing is not null) return Fail($"The stock record of '{missing}' is missing.", "STOCK_RECORD_MISSING");

        var cashier = await _users.GetByIdAsync(userId, ct);
        var nextNumber = await _numbers.PrepareAsync(ReturnCounterKey, "IA", ct);

        QuickSaleReturn saleReturn;
        try
        {
            saleReturn = QuickSaleReturn.Create(
                nextNumber, sale, _clock.GetUtcNow().UtcDateTime, userId, cashier?.Name ?? "Unknown", shift.Id,
                request.Reason, refundMethod, request.Items.Select(item => (item.LineId, item.Quantity)).ToList());
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Fail(exception.Message, "RETURN_REJECTED");
        }

        foreach (var group in saleReturn.Lines.Where(line => line.TracksStock && line.ProductCode is not null).GroupBy(line => line.ProductCode!))
        {
            _inventory.ApplyMovement(
                stocks[group.Key], group.Sum(line => line.Quantity), StockMovementType.Return,
                $"Return {saleReturn.ReturnNumber} of sale {sale.SaleNumber}");
        }

        _sales.AddReturn(saleReturn);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);

        return Result<QuickSaleDto>.Ok(await MapAsync(sale, ct));
    }

    // ---- helpers -----------------------------------------------------------------------------------------

    /// <summary>Managers and administrators see every sale; anyone else sees only their own.</summary>
    private bool SeesEveryCashier => _currentUser.Roles.Any(role => role is "Admin" or "Manager");

    private bool CanSee(QuickSale sale) => SeesEveryCashier || sale.CashierUserId == _currentUser.UserId;

    private async Task<Dictionary<string, MaterialStock>> LoadStockAsync(IEnumerable<string> codes, CancellationToken ct)
    {
        var wanted = codes.Distinct().ToList();
        var rows = await _inventory.GetByMaterialCodesAsync(wanted, ct);
        return rows.ToDictionary(row => row.MaterialCode);
    }

    private static bool TryParseMethod(string? value, out SalePaymentMethod method)
        => Enum.TryParse(value, ignoreCase: true, out method) && Enum.IsDefined(method);

    private static Result<QuickSaleDto> Fail(string error, string? errorCode = null) => Result<QuickSaleDto>.Fail(error, errorCode);

    private async Task<QuickSaleDto> MapAsync(QuickSale sale, CancellationToken ct)
    {
        string? customerName = null;
        if (sale.CustomerId is { } customerId) customerName = (await _customers.GetByIdAsync(customerId, ct))?.FullName;
        return Map(sale, customerName, await _sales.GetReturnsAsync(sale.Id, ct));
    }

    private static QuickSaleDto Map(QuickSale sale, string? customerName, IReadOnlyList<QuickSaleReturn> returns)
    {
        var lineNumbers = sale.Lines.ToDictionary(line => line.Id, line => line.LineNumber);
        return new QuickSaleDto(
            sale.Id, sale.SaleNumber, sale.SoldAt, sale.CashierUserId, sale.CashierName, sale.ShiftId, sale.CustomerId, customerName,
            sale.Status.ToString(), sale.Subtotal, sale.LineDiscountTotal, sale.ReceiptDiscount, sale.GrandTotal, sale.VatTotal,
            sale.CashTendered, sale.ChangeGiven, sale.Note, sale.VoidedAt, sale.VoidReason,
            sale.Lines.OrderBy(line => line.LineNumber).Select(MapLine).ToList(),
            sale.Payments.OrderBy(payment => payment.Method).Select(MapPayment).ToList(),
            returns.Select(saleReturn => MapReturn(saleReturn, lineNumbers)).ToList());
    }

    private static QuickSaleLineDto MapLine(QuickSaleLine line) => new(
        line.Id, line.ProductId, line.ProductCode, line.Barcode, line.Description, line.Unit, line.Quantity, line.UnitPrice,
        line.VatRate, line.LineDiscount, line.ReceiptDiscountShare, line.LineTotal, line.VatAmount, line.ReturnedQuantity);

    private static QuickSalePaymentDto MapPayment(QuickSalePayment payment)
        => new(payment.Method.ToString(), payment.Amount, payment.Tendered, payment.Reference);

    private static QuickSaleReturnDto MapReturn(QuickSaleReturn saleReturn, IReadOnlyDictionary<Guid, int> lineNumbers) => new(
        saleReturn.Id, saleReturn.ReturnNumber, saleReturn.ReturnedAt, saleReturn.CashierName, saleReturn.Reason,
        saleReturn.RefundMethod.ToString(), saleReturn.RefundTotal,
        saleReturn.Lines
            .OrderBy(line => lineNumbers.GetValueOrDefault(line.QuickSaleLineId))
            .Select(line => new QuickSaleReturnLineDto(line.QuickSaleLineId, line.ProductCode, line.Description, line.Quantity, line.RefundAmount))
            .ToList());

    private static QuickSaleSummaryDto MapSummary(QuickSale sale) => new(
        sale.Id, sale.SaleNumber, sale.SoldAt, sale.CashierName, sale.CustomerId, sale.Status.ToString(), sale.GrandTotal, sale.VatTotal,
        sale.Payments.Select(payment => payment.Method).Distinct().Order().Select(method => method.ToString()).ToList(),
        sale.Lines.Any(line => line.ReturnedQuantity > 0));
}
