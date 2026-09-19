using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Inventory;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class ProductService : IProductService
{
    private const int MaxCodeLength = 64;
    private const int MaxNameLength = 200;
    private const int MaxBarcodeLength = 64;
    private const int MaxUnitLength = 16;
    private const decimal MaxPrice = 100_000_000m;

    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;

    public ProductService(IProductRepository products, IInventoryRepository inventory, ICommandJournal commandJournal, IOperationContext operationContext)
    {
        _products = products;
        _inventory = inventory;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
    }

    public async Task<Result<PagedResult<ProductDto>>> ListAsync(string? search, bool activeOnly, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var page = await _products.ListPagedAsync(
            search, activeOnly, PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        var stock = await StockAvailabilityAsync(page.Items, ct);
        return Result<PagedResult<ProductDto>>.Ok(page.Map(product => Map(product, stock)));
    }

    public async Task<Result<ProductDto>> LookupAsync(string term, CancellationToken ct = default)
    {
        var product = await _products.FindAsync(term ?? string.Empty, ct);
        if (product is null || !product.IsActive) return Result<ProductDto>.Fail("Product not found.", "PRODUCT_NOT_FOUND");
        return Result<ProductDto>.Ok(Map(product, await StockAvailabilityAsync([product], ct)));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code)) return Result<ProductDto>.Fail("Code is required.");
        if (request.Code.Trim().Length > MaxCodeLength) return Result<ProductDto>.Fail($"Code can be at most {MaxCodeLength} characters.");
        var invalid = ValidateFields(request.Name, request.Barcode, request.Unit, request.SalePrice, request.VatRate);
        if (invalid is not null) return Result<ProductDto>.Fail(invalid);

        if (await _products.GetByCodeAsync(request.Code, ct) is not null)
            return Result<ProductDto>.Fail("A product with this code already exists.", "PRODUCT_CODE_EXISTS");
        if (!string.IsNullOrWhiteSpace(request.Barcode) && await _products.GetByBarcodeAsync(request.Barcode, ct) is not null)
            return Result<ProductDto>.Fail("A product with this barcode already exists.", "PRODUCT_BARCODE_EXISTS");

        var product = new Product(request.Code, request.Name, request.Barcode, request.Unit, request.SalePrice, request.VatRate, request.TracksStock);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _products.AddAsync(product, ct);
        return Result<ProductDto>.Ok(Map(product, await StockAvailabilityAsync([product], ct)));
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var invalid = ValidateFields(request.Name, request.Barcode, request.Unit, request.SalePrice, request.VatRate);
        if (invalid is not null) return Result<ProductDto>.Fail(invalid);

        var product = await _products.GetByIdAsync(id, ct);
        if (product is null) return Result<ProductDto>.Fail("Product not found.", "PRODUCT_NOT_FOUND");

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var other = await _products.GetByBarcodeAsync(request.Barcode, ct);
            if (other is not null && other.Id != product.Id)
                return Result<ProductDto>.Fail("A product with this barcode already exists.", "PRODUCT_BARCODE_EXISTS");
        }

        product.Update(request.Name, request.Barcode, request.Unit, request.SalePrice, request.VatRate, request.TracksStock, request.IsActive);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _products.UpdateAsync(product, ct);
        return Result<ProductDto>.Ok(Map(product, await StockAvailabilityAsync([product], ct)));
    }

    private static string? ValidateFields(string? name, string? barcode, string? unit, decimal price, decimal vatRate)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Name is required.";
        if (name.Trim().Length > MaxNameLength) return $"Name can be at most {MaxNameLength} characters.";
        if (barcode is not null && barcode.Trim().Length > MaxBarcodeLength) return $"Barcode can be at most {MaxBarcodeLength} characters.";
        if (unit is not null && unit.Trim().Length > MaxUnitLength) return $"Unit can be at most {MaxUnitLength} characters.";
        if (price < 0 || price > MaxPrice) return $"Sale price must be between 0 and {MaxPrice:N0}.";
        if (vatRate < 0 || vatRate > 100) return "VAT rate must be between 0 and 100.";
        return null;
    }

    private async Task<IReadOnlyDictionary<string, decimal>> StockAvailabilityAsync(IEnumerable<Product> products, CancellationToken ct)
    {
        var codes = products.Where(product => product.TracksStock).Select(product => product.Code).Distinct().ToList();
        var rows = await _inventory.GetByMaterialCodesAsync(codes, ct);
        return rows.ToDictionary(row => row.MaterialCode, row => row.AvailableQuantity);
    }

    private static ProductDto Map(Product product, IReadOnlyDictionary<string, decimal> stock) =>
        new(product.Id, product.Code, product.Name, product.Barcode, product.Unit, product.SalePrice, product.VatRate,
            product.TracksStock, product.IsActive,
            product.TracksStock ? stock.GetValueOrDefault(product.Code) : (decimal?)null);
}
