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


