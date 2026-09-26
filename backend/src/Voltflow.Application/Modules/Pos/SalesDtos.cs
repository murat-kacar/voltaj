using Voltflow.Domain.Sales;
using Voltflow.Application.Common;

namespace Voltflow.Application.Dtos;

public sealed record QuickSaleLineDto(
    Guid? ProductId,
    string? ProductCode,
    string? Barcode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal DiscountAmount,
    decimal TotalAmount);

public sealed record QuickSalePaymentDto(
    SalePaymentMethod Method,
    decimal Amount,
    string? Reference);

public sealed record QuickSaleDto(
    Guid Id,
    string SaleNumber,
    DateTime SoldAt,
    Guid CashierUserId,
    string CashierName,
    Guid ShiftId,
    Guid? CustomerId,
    string Status,
    decimal Subtotal,
    decimal LineDiscountTotal,
    decimal ReceiptDiscount,
    decimal GrandTotal,
    decimal VatTotal,
    decimal CashTendered,
    decimal ChangeGiven,
    string? Note,
    IReadOnlyList<QuickSaleLineDto> Lines,
    IReadOnlyList<QuickSalePaymentDto> Payments);

public sealed record QuickSaleSummaryDto(
    Guid Id,
    string SaleNumber,
    DateTime SoldAt,
    string CashierName,
    Guid? CustomerId,
    string Status,
    decimal GrandTotal);

public sealed record StartQuickSaleRequest(
    Guid ShiftId,
    Guid? CustomerId,
    decimal ReceiptDiscount,
    string? Note,
    List<QuickSaleLineInput> Lines,
    List<QuickSalePaymentInput> Payments);

public sealed record VoidQuickSaleRequest(string Reason);
