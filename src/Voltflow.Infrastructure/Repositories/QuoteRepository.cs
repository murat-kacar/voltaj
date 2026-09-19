using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Quotes;
using Voltflow.Domain.WorkOrders;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class QuoteRepository : Repository<Quote>, IQuoteRepository
{
    public QuoteRepository(VoltflowDbContext dbContext) : base(dbContext) { }

    public override Task<Quote?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => DbContext.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Quote?> GetByNumberAsync(string number, CancellationToken ct = default)
        => DbContext.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Number == number, ct);

    public async Task<IReadOnlyList<Quote>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
        => await DbContext.Quotes.Include(x => x.Items).Where(x => x.CustomerId == customerId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<WorkOrder> ConvertAcceptedToWorkOrderAsync(Guid quoteId, CancellationToken ct = default)
    {
        var isInMemory = DbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        var transaction = isInMemory ? null : await DbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var quote = await GetByIdAsync(quoteId, ct) ?? throw new InvalidOperationException("Quote not found.");
            var existing = await DbContext.WorkOrders.SingleOrDefaultAsync(x => x.SourceQuoteId == quoteId, ct);
            if (existing is not null)
            {
                if (transaction is not null) await transaction.CommitAsync(ct);
                return existing;
            }

            if (quote.State != QuoteState.Accepted)
                throw new InvalidOperationException("Only accepted quotes can create work orders.");

            var workOrder = new WorkOrder(quote.CustomerId, quote.Title);
            workOrder.LinkSourceQuote(quote.Id);
            if (quote.SiteId is { } siteId) workOrder.LinkToSiteAndAsset(siteId, quote.AssetId);
            foreach (var item in quote.Items.OrderBy(candidate => candidate.LineNumber))
                workOrder.AddItem(item.Description, item.Quantity, item.UnitPrice);

            await DbContext.WorkOrders.AddAsync(workOrder, ct);
            await DbContext.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return workOrder;
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public override async Task<IReadOnlyList<Quote>> ListAsync(CancellationToken ct = default)
        => await DbContext.Quotes.Include(x => x.Items).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<PagedResult<Quote>> ListPagedAsync(QuoteFilter filter, int limit, int offset, CancellationToken ct = default)
    {
        // the list shows totals, which the quote keeps itself, so the lines are not loaded
        var query = DbContext.Quotes.AsNoTracking().AsQueryable();
        if (filter.CustomerId is { } customerId) query = query.Where(x => x.CustomerId == customerId);
        if (filter.State is { } state) query = query.Where(x => x.State == state);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            var customerIds = DbContext.Customers.Where(x => x.FullName.ToLower().Contains(term)).Select(x => x.Id);
            query = query.Where(x => x.Number.ToLower().Contains(term) || x.Title.ToLower().Contains(term) || customerIds.Contains(x.CustomerId));
        }

        var ordered = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Number);
        var total = await ordered.CountAsync(ct);
        var items = await ordered.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<Quote>(items, total, limit, offset);
    }

    public Task<WorkOrder?> GetWorkOrderAsync(Guid quoteId, CancellationToken ct = default)
        => DbContext.WorkOrders.AsNoTracking().FirstOrDefaultAsync(x => x.SourceQuoteId == quoteId, ct);

    public async Task<IReadOnlyList<Quote>> ListDueForExpiryAsync(DateOnly today, CancellationToken ct = default)
        => await DbContext.Quotes.Where(x => x.State == QuoteState.Issued && x.ValidUntil != null && x.ValidUntil < today).ToListAsync(ct);
}
