using Microsoft.EntityFrameworkCore;
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
            foreach (var item in quote.Items)
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
}
