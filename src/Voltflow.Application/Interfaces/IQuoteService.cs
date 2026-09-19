using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IQuoteService
{
    Task<Result<QuoteDto>> CreateAsync(CreateQuoteRequest request, CancellationToken ct = default);
    Task<Result<QuoteDto>> UpdateAsync(Guid id, UpdateQuoteRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<QuoteSummaryDto>>> ListAsync(
        string? search = null, string? state = null, Guid? customerId = null, int? limit = null, int? offset = null, CancellationToken ct = default);
    Task<Result<QuoteDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<QuoteDto>> AddItemAsync(Guid id, QuoteLineRequest request, CancellationToken ct = default);
    Task<Result<QuoteDto>> CopyAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<QuoteDto>> IssueAsync(Guid id, CancellationToken ct = default);
    Task<Result<QuoteDto>> AcceptAsync(Guid id, AcceptQuoteRequest request, CancellationToken ct = default);
    Task<Result<QuoteDto>> PayDepositAsync(Guid id, PayQuoteDepositRequest request, CancellationToken ct = default);
    Task<Result<QuoteDto>> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<Result<QuoteDto>> ExpireAsync(Guid id, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> ConvertAcceptedToWorkOrderAsync(Guid id, CancellationToken ct = default);
}
