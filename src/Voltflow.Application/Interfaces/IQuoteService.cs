using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IQuoteService
{
    Task<Result<QuoteDto>> CreateAsync(CreateQuoteRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<QuoteDto>>> ListAsync(Guid? customerId = null, CancellationToken ct = default);
    Task<Result<QuoteDto>> AddItemAsync(Guid id, AddQuoteItemRequest request, CancellationToken ct = default);
    Task<Result<QuoteDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<QuoteDto>>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<QuoteDto>> IssueAsync(Guid id, CancellationToken ct = default);
    Task<Result<QuoteDto>> AcceptAsync(Guid id, AcceptQuoteRequest request, CancellationToken ct = default);
    Task<Result<QuoteDto>> PayDepositAsync(Guid id, PayQuoteDepositRequest request, CancellationToken ct = default);
    Task<Result<QuoteDto>> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<Result<QuoteDto>> ExpireAsync(Guid id, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> ConvertAcceptedToWorkOrderAsync(Guid id, CancellationToken ct = default);
}
