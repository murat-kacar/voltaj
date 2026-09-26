using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IServiceService
{
    // ---- reading ----
    Task<Result<ServiceDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<ServiceSummaryDto>>> ListAsync(
        string? search = null, string? status = null,
        Guid? customerId = null, Guid? assignedUserId = null,
        int? limit = null, int? offset = null,
        CancellationToken ct = default);

    // ---- draft phase ----
    Task<Result<ServiceDto>> CreateDraftAsync(CreateServiceDraftRequest request, CancellationToken ct = default);
    Task<Result<ServiceDto>> UpdateDraftAsync(Guid id, UpdateServiceDraftRequest request, CancellationToken ct = default);
    Task<Result<ServiceDto>> AddItemAsync(Guid id, ServiceLineRequest request, CancellationToken ct = default);
    Task<Result<ServiceDto>> RemoveItemAsync(Guid id, Guid itemId, string auditNote, CancellationToken ct = default);
    Task<Result<ServiceDto>> ReviseAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeleteDraftAsync(Guid id, CancellationToken ct = default);

    // ---- decisions ----
    Task<Result<ServiceDto>> IssueAsync(Guid id, CancellationToken ct = default);
    Task<Result<ServiceDto>> AcceptAsync(Guid id, AcceptServiceRequest request, CancellationToken ct = default);
    Task<Result<ServiceDto>> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<Result<ServiceDto>> CancelAsync(Guid id, string? reason, CancellationToken ct = default);

    // ---- active phase operations ----
    Task<Result<ServiceDto>> AssignAsync(Guid id, AssignServiceRequest request, CancellationToken ct = default);
    Task<Result<ServiceDto>> HoldAsync(Guid id, HoldServiceRequest request, CancellationToken ct = default);
    Task<Result<ServiceDto>> ResumeAsync(Guid id, CancellationToken ct = default);
    Task<Result<ServiceDto>> CompleteAsync(Guid id, CompleteServiceRequest request, CancellationToken ct = default);
    Task<Result<ServiceDto>> UpdateSubStatusAsync(Guid id, UpdateSubStatusRequest request, CancellationToken ct = default);
    
    // ---- financial ----
    Task<Result<ServiceDto>> PayDepositAsync(Guid id, PayServiceDepositRequest request, CancellationToken ct = default);
    Task<Result<ServiceDto>> IssuePartialInvoiceAsync(Guid id, IssuePartialInvoiceRequest request, CancellationToken ct = default);
}
