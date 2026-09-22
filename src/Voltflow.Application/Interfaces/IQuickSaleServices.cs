using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IProductService
{
    Task<Result<PagedResult<ProductDto>>> ListAsync(string? search, bool activeOnly, int? limit = null, int? offset = null, CancellationToken ct = default);

    /// <summary>Finds the active product a scanner or a typed code points at: an exact barcode first, then an exact code.</summary>
    Task<Result<ProductDto>> LookupAsync(string term, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ProductDto>>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
}

public interface IQuickSaleService
{
    Task<Result<QuickSaleDto>> CreateAsync(CreateQuickSaleRequest request, CancellationToken ct = default);
    Task<Result<QuickSaleDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<QuickSaleSummaryDto>>> ListAsync(string? search, string? status, DateTime? from, DateTime? to, Guid? customerId = null, int? limit = null, int? offset = null, CancellationToken ct = default);
    Task<Result<QuickSaleDto>> VoidAsync(Guid id, VoidQuickSaleRequest request, CancellationToken ct = default);
    Task<Result<QuickSaleDto>> ReturnAsync(Guid id, ReturnQuickSaleRequest request, CancellationToken ct = default);
}

public interface ICashShiftService
{
    /// <summary>The signed-in user's open shift with its running figures, or null when there is none.</summary>
    Task<Result<CashShiftReportDto?>> GetCurrentAsync(CancellationToken ct = default);

    Task<Result<CashShiftReportDto>> OpenAsync(OpenShiftRequest request, CancellationToken ct = default);
    Task<Result<CashShiftReportDto>> CloseAsync(Guid id, CloseShiftRequest request, CancellationToken ct = default);
    Task<Result<CashShiftReportDto>> GetReportAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<CashShiftDto>>> ListAsync(int? limit = null, int? offset = null, CancellationToken ct = default);
}
