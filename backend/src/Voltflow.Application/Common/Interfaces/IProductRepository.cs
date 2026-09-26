using Voltflow.Application.Common;
using Voltflow.Domain.Inventory;

namespace Voltflow.Application.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    /// <summary>Case-insensitive; codes that differ only in case are the same product.</summary>
    Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>What a scanner or a typed code should find: an exact barcode first, then an exact code.</summary>
    Task<Product?> FindAsync(string term, CancellationToken ct = default);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<PagedResult<Product>> ListPagedAsync(string? search, bool activeOnly, int limit, int offset, CancellationToken ct = default);
}
