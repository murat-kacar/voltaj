namespace Voltflow.Application.Interfaces;

using Voltflow.Domain.Common;
using Voltflow.Application.Common;

public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ICustomerRepository : IRepository<Voltflow.Domain.Customers.Customer>
{
    Task<Voltflow.Domain.Customers.Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Voltflow.Domain.Customers.Customer?> GetByTaxNumberAsync(string taxNumber, CancellationToken ct = default);
    /// <summary>V8: the only list surface the Customers endpoint should call going forward.</summary>
    Task<PagedResult<Voltflow.Domain.Customers.Customer>> ListPagedAsync(int limit, int offset, CancellationToken ct = default);
}

public interface IQuoteRepository : IRepository<Voltflow.Domain.Quotes.Quote>
{
    Task<Voltflow.Domain.Quotes.Quote?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<Voltflow.Domain.Quotes.Quote>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Voltflow.Domain.WorkOrders.WorkOrder> ConvertAcceptedToWorkOrderAsync(Guid quoteId, CancellationToken ct = default);
    /// <summary>V8: unifies the filtered/unfiltered list; customerId null means no filter.</summary>
    Task<PagedResult<Voltflow.Domain.Quotes.Quote>> ListPagedAsync(int limit, int offset, Guid? customerId, CancellationToken ct = default);
}
