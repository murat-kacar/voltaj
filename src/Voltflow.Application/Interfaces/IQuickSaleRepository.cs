using Voltflow.Application.Common;
using Voltflow.Domain.Sales;

namespace Voltflow.Application.Interfaces;

public interface IQuickSaleRepository : IRepository<QuickSale>
{
    Task<PagedResult<QuickSale>> ListPagedAsync(int limit, int offset, CancellationToken ct = default);
}
