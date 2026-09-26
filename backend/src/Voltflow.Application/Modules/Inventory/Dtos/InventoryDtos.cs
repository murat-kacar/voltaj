namespace Voltflow.Application.Dtos;

public sealed record AdjustStockRequest(string MaterialCode, decimal Delta);
public sealed record ReserveStockRequest(string MaterialCode, decimal Quantity);
public sealed record StockDto(string MaterialCode, string Name, decimal QuantityOnHand, decimal ReservedQuantity, decimal AvailableQuantity);

public sealed record ReceiveGoodsLineRequest(string MaterialCode, decimal Quantity, decimal UnitPrice);
public sealed record ReceiveGoodsRequest(string SupplierName, string InvoiceNumber, DateTime InvoiceDate, List<ReceiveGoodsLineRequest> Lines);
