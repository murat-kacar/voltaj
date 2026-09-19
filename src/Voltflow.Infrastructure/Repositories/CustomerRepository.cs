using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(VoltflowDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var key = email.Trim().ToLowerInvariant();
        return DbContext.Customers.FirstOrDefaultAsync(x => x.Email.ToLower() == key, ct);
    }

    public Task<Customer?> GetByTaxNumberAsync(string taxNumber, CancellationToken ct = default)
    {
        var key = taxNumber.Trim();
        return DbContext.Customers.FirstOrDefaultAsync(x => x.TaxNumber == key, ct);
    }

    public override async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken ct = default)
        => await DbContext.Customers.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);

    public async Task<PagedResult<Customer>> ListPagedAsync(CustomerFilter filter, int limit, int offset, CancellationToken ct = default)
    {
        var query = DbContext.Customers.AsNoTracking().AsQueryable();
        if (filter.Type is { } type) query = query.Where(x => x.Type == type);
        if (filter.IsActive is { } active) query = query.Where(x => x.IsActive == active);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.FullName.ToLower().Contains(term)
                                     || x.Email.ToLower().Contains(term)
                                     || x.Phone.Contains(term)
                                     || x.TaxNumber.ToLower().Contains(term));
        }

        var ordered = query.OrderBy(x => x.FullName).ThenBy(x => x.Id);
        var total = await ordered.CountAsync(ct);
        var items = await ordered.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<Customer>(items, total, limit, offset);
    }
}
