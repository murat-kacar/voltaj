namespace Voltflow.Application.Interfaces;

using Voltflow.Domain.Common;

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
}

public interface IQuoteRepository : IRepository<Voltflow.Domain.Quotes.Quote>
{
    Task<Voltflow.Domain.Quotes.Quote?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<Voltflow.Domain.Quotes.Quote>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Voltflow.Domain.WorkOrders.WorkOrder> ConvertAcceptedToWorkOrderAsync(Guid quoteId, CancellationToken ct = default);
}
