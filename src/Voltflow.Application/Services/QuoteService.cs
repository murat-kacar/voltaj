using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Quotes;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class QuoteService : IQuoteService
{
    private readonly IQuoteRepository _quoteRepository;

    public QuoteService(IQuoteRepository quoteRepository)
    {
        _quoteRepository = quoteRepository;
    }

    public async Task<Result<QuoteDto>> CreateAsync(CreateQuoteRequest request, CancellationToken ct = default)
    {
        if (request.CustomerId == Guid.Empty) return Result<QuoteDto>.Fail("CustomerId is required.");
        if (string.IsNullOrWhiteSpace(request.Title)) return Result<QuoteDto>.Fail("Title is required.");

        var quote = new Quote(request.CustomerId, request.Title.Trim());
        await _quoteRepository.AddAsync(quote, ct);

        return Result<QuoteDto>.Ok(Map(quote));
    }

    public async Task<Result<IReadOnlyList<QuoteDto>>> ListAsync(Guid? customerId = null, CancellationToken ct = default)
    {
        var quotes = customerId is null
            ? await _quoteRepository.ListAsync(ct)
            : await _quoteRepository.GetByCustomerAsync(customerId.Value, ct);
        return Result<IReadOnlyList<QuoteDto>>.Ok(quotes.Select(Map).ToList());
    }

    public async Task<Result<QuoteDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var quote = await _quoteRepository.GetByIdAsync(id, ct);
        if (quote is null) return Result<QuoteDto>.Fail("Quote not found.");

        return Result<QuoteDto>.Ok(Map(quote));
    }

    public async Task<Result<IReadOnlyList<QuoteDto>>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var quotes = await _quoteRepository.GetByCustomerAsync(customerId, ct);
        return Result<IReadOnlyList<QuoteDto>>.Ok(quotes.Select(Map).ToList());
    }

    public Task<Result<QuoteDto>> AddItemAsync(Guid id, AddQuoteItemRequest request, CancellationToken ct = default)
        => ExecuteActionAsync(id, quote =>
        {
            if (string.IsNullOrWhiteSpace(request.Description)) throw new InvalidOperationException("Description is required.");
            if (request.Quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");
            if (request.UnitPrice < 0) throw new InvalidOperationException("UnitPrice cannot be negative.");
            
            quote.AddItem(request.Description.Trim(), request.Quantity, request.UnitPrice);
        }, ct);

    public Task<Result<QuoteDto>> IssueAsync(Guid id, CancellationToken ct = default)
        => ExecuteActionAsync(id, quote => quote.Issue(), ct);

    public Task<Result<QuoteDto>> AcceptAsync(Guid id, AcceptQuoteRequest request, CancellationToken ct = default)
        => ExecuteActionAsync(id, quote => quote.Accept(request.RequiredDepositPercentage), ct);

    public Task<Result<QuoteDto>> PayDepositAsync(Guid id, PayQuoteDepositRequest request, CancellationToken ct = default)
        => ExecuteActionAsync(id, quote => quote.PayDeposit(request.Amount), ct);

    public Task<Result<QuoteDto>> RejectAsync(Guid id, string reason, CancellationToken ct = default)
        => ExecuteActionAsync(id, quote => quote.Reject(reason), ct);

    public Task<Result<QuoteDto>> ExpireAsync(Guid id, CancellationToken ct = default)
        => ExecuteActionAsync(id, quote => quote.Expire(), ct);

    public async Task<Result<WorkOrderDto>> ConvertAcceptedToWorkOrderAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var workOrder = await _quoteRepository.ConvertAcceptedToWorkOrderAsync(id, ct);
            return Result<WorkOrderDto>.Ok(WorkOrderDto.MapFrom(workOrder));
        }
        catch (InvalidOperationException ex)
        {
            return Result<WorkOrderDto>.Fail(ex.Message);
        }
    }

    private async Task<Result<QuoteDto>> ExecuteActionAsync(Guid id, Action<Quote> action, CancellationToken ct)
    {
        var quote = await _quoteRepository.GetByIdAsync(id, ct);
        if (quote is null) return Result<QuoteDto>.Fail("Quote not found.");

        try
        {
            action(quote);
            await _quoteRepository.UpdateAsync(quote, ct);
            return Result<QuoteDto>.Ok(Map(quote));
        }
        catch (InvalidOperationException ex)
        {
            return Result<QuoteDto>.Fail(ex.Message);
        }
    }

    private static QuoteDto Map(Quote quote) => new(
        quote.Id, quote.CustomerId, quote.Number, quote.Title, quote.Total, quote.State.ToString(),
        quote.RejectionReason, quote.RequiredDepositPercentage, quote.DepositPaidAmount);
}
