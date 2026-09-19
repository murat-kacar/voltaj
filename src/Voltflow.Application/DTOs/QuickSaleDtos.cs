namespace Voltflow.Application.Dtos;

// ---- price list ---------------------------------------------------------------------------------------------

/// <param name="StockAvailable">What can still be sold; null when the product does not track stock.</param>
public sealed record ProductDto(
    Guid Id,
    string Code,
    string Name,
    string? Barcode,
    string Unit,
    decimal SalePrice,
    decimal VatRate,
    bool TracksStock,
    bool IsActive,
    decimal? StockAvailable);

public sealed record CreateProductRequest(
    string Code,
    string Name,
    string? Barcode,
    string? Unit,
    decimal SalePrice,
    decimal VatRate,
    bool TracksStock);

public sealed record UpdateProductRequest(
    string Name,
    string? Barcode,
    string? Unit,
    decimal SalePrice,
    decimal VatRate,
    bool TracksStock,
    bool IsActive);

// ---- sales --------------------------------------------------------------------------------------------------

/// <summary>A line from the price list (<see cref="ProductId"/> set: name, price and VAT come from the product) or a free line
/// (no product: description, unit price and VAT rate are required and no stock moves).</summary>
public sealed record QuickSaleLineRequest(
    Guid? ProductId,
    string? Description,
    decimal Quantity,
    decimal? UnitPrice,
    decimal? VatRate,
    decimal DiscountAmount);

/// <param name="Method">Cash, Card or BankTransfer. For cash, Amount is what the customer handed over; the change is worked out.</param>
public sealed record QuickSalePaymentRequest(string Method, decimal Amount, string? Reference);

public sealed record CreateQuickSaleRequest(
    Guid? CustomerId,
    IReadOnlyList<QuickSaleLineRequest> Lines,
    decimal ReceiptDiscount,
    IReadOnlyList<QuickSalePaymentRequest> Payments,
    string? Note);

public sealed record VoidQuickSaleRequest(string Reason);

public sealed record ReturnItemRequest(Guid LineId, decimal Quantity);

/// <param name="RefundMethod">Cash, Card or BankTransfer; only a cash refund leaves the drawer.</param>
public sealed record ReturnQuickSaleRequest(string Reason, string RefundMethod, IReadOnlyList<ReturnItemRequest> Items);

public sealed record QuickSaleLineDto(
    Guid Id,
    Guid? ProductId,
    string? ProductCode,
    string? Barcode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineDiscount,
    decimal ReceiptDiscountShare,
    decimal LineTotal,
    decimal VatAmount,
    decimal ReturnedQuantity);

public sealed record QuickSalePaymentDto(string Method, decimal Amount, decimal Tendered, string? Reference);

public sealed record QuickSaleReturnLineDto(Guid QuickSaleLineId, string? ProductCode, string Description, decimal Quantity, decimal RefundAmount);

public sealed record QuickSaleReturnDto(
    Guid Id,
    string ReturnNumber,
    DateTime ReturnedAt,
    string CashierName,
    string Reason,
    string RefundMethod,
    decimal RefundTotal,
    IReadOnlyList<QuickSaleReturnLineDto> Lines);

public sealed record QuickSaleDto(
    Guid Id,
    string SaleNumber,
    DateTime SoldAt,
    Guid CashierUserId,
    string CashierName,
    Guid ShiftId,
    Guid? CustomerId,
    string? CustomerName,
    string Status,
    decimal Subtotal,
    decimal LineDiscountTotal,
    decimal ReceiptDiscount,
    decimal GrandTotal,
    decimal VatTotal,
    decimal CashTendered,
    decimal ChangeGiven,
    string? Note,
    DateTime? VoidedAt,
    string? VoidReason,
    IReadOnlyList<QuickSaleLineDto> Lines,
    IReadOnlyList<QuickSalePaymentDto> Payments,
    IReadOnlyList<QuickSaleReturnDto> Returns);

public sealed record QuickSaleSummaryDto(
    Guid Id,
    string SaleNumber,
    DateTime SoldAt,
    string CashierName,
    Guid? CustomerId,
    string Status,
    decimal GrandTotal,
    decimal VatTotal,
    IReadOnlyList<string> PaymentMethods,
    bool HasReturns);

// ---- shifts -------------------------------------------------------------------------------------------------

public sealed record OpenShiftRequest(decimal OpeningCash);

public sealed record CloseShiftRequest(decimal CountedCash, string? Note);

public sealed record CashShiftDto(
    Guid Id,
    Guid CashierUserId,
    string CashierName,
    DateTime OpenedAt,
    decimal OpeningCash,
    string Status,
    DateTime? ClosedAt,
    decimal? CountedCash,
    decimal? ExpectedCash,
    decimal? CashDifference,
    string? Note);

public sealed record VatBucketDto(decimal Rate, decimal Gross, decimal Vat);

/// <summary>The figures of one shift. <see cref="VatTotal"/> and <see cref="VatBreakdown"/> describe the sales; the returns are
/// shown separately and do not reduce them.</summary>
public sealed record CashShiftReportDto(
    CashShiftDto Shift,
    int SaleCount,
    int VoidedCount,
    decimal SalesTotal,
    decimal DiscountTotal,
    decimal VatTotal,
    decimal CashSales,
    decimal CardSales,
    decimal TransferSales,
    int ReturnCount,
    decimal ReturnTotal,
    decimal CashRefunds,
    decimal NetSales,
    decimal ExpectedCash,
    IReadOnlyList<VatBucketDto> VatBreakdown);
