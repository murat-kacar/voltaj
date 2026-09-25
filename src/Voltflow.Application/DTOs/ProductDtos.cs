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


