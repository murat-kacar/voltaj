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

/// <summary>What the customer list is narrowed by; a null part does not narrow.</summary>
public sealed record CustomerFilter(string? Search, Voltflow.Domain.Customers.CustomerType? Type, bool? IsActive);

public interface ICustomerRepository : IRepository<Voltflow.Domain.Customers.Customer>
{
    Task<Voltflow.Domain.Customers.Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Voltflow.Domain.Customers.Customer?> GetByTaxNumberAsync(string taxNumber, CancellationToken ct = default);
    /// <summary>The names of these customers, for screens that list documents.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    /// <summary>V8: the only list surface the Customers endpoint should call going forward.</summary>
    Task<PagedResult<Voltflow.Domain.Customers.Customer>> ListPagedAsync(CustomerFilter filter, int limit, int offset, CancellationToken ct = default);
}

/// <summary>What the quote list is narrowed by; a null part does not narrow.</summary>
public sealed record QuoteFilter(string? Search, Voltflow.Domain.Quotes.QuoteState? State, Guid? CustomerId);

public interface IQuoteRepository : IRepository<Voltflow.Domain.Quotes.Quote>
{
    Task<Voltflow.Domain.Quotes.Quote?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<Voltflow.Domain.Quotes.Quote>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Voltflow.Domain.WorkOrders.WorkOrder> ConvertAcceptedToWorkOrderAsync(Guid quoteId, CancellationToken ct = default);
    /// <summary>The list of the quotes screen: newest first, without the lines.</summary>
    Task<PagedResult<Voltflow.Domain.Quotes.Quote>> ListPagedAsync(QuoteFilter filter, int limit, int offset, CancellationToken ct = default);
    /// <summary>The work order made from the quote, if it has been converted.</summary>
    Task<Voltflow.Domain.WorkOrders.WorkOrder?> GetWorkOrderAsync(Guid quoteId, CancellationToken ct = default);
    /// <summary>The issued quotes whose last day of validity is before <paramref name="today"/>.</summary>
    Task<IReadOnlyList<Voltflow.Domain.Quotes.Quote>> ListDueForExpiryAsync(DateOnly today, CancellationToken ct = default);
}
