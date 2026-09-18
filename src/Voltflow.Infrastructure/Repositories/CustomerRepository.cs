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
        => DbContext.Customers.FirstOrDefaultAsync(x => x.Email == email, ct);

    public Task<Customer?> GetByTaxNumberAsync(string taxNumber, CancellationToken ct = default)
        => DbContext.Customers.FirstOrDefaultAsync(x => x.TaxNumber == taxNumber, ct);

    public override async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken ct = default)
        => await DbContext.Customers.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);

    public async Task<PagedResult<Customer>> ListPagedAsync(int limit, int offset, CancellationToken ct = default)
    {
        var query = DbContext.Customers.AsNoTracking().OrderBy(x => x.FullName);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<Customer>(items, total, limit, offset);
    }
}

