using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Projects;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class BillingService : IBillingService
{
    private readonly IBillingRepository _repository;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;

    public BillingService(IBillingRepository repository, ICommandJournal commandJournal, IOperationContext operationContext)
    {
        _repository = repository;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
    }

    public async Task<Result<BillingEntryDto>> CreateAsync(Guid projectId, Guid customerId, decimal amount, CancellationToken ct = default)
    {
        if (projectId == Guid.Empty) return Result<BillingEntryDto>.Fail("ProjectId is required.");
        if (customerId == Guid.Empty) return Result<BillingEntryDto>.Fail("CustomerId is required.");
        if (amount <= 0) return Result<BillingEntryDto>.Fail("Amount must be greater than zero.");
        var entry = new BillingEntry(projectId, customerId, amount);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _repository.AddAsync(entry, ct);
        return Result<BillingEntryDto>.Ok(new BillingEntryDto(entry.Id, entry.ProjectId, entry.CustomerId, entry.Amount));
    }

    public Task<Result<BillingEntryDto>> CreateAsync(Guid projectId, CreateBillingEntryRequest request, CancellationToken ct = default)
        => CreateAsync(projectId, request.CustomerId, request.Amount, ct);

    public async Task<Result<PagedResult<BillingEntryDto>>> ListAsync(Guid projectId, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var page = await _repository.GetByProjectPagedAsync(
            projectId, PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        return Result<PagedResult<BillingEntryDto>>.Ok(page.Map(x => new BillingEntryDto(x.Id, x.ProjectId, x.CustomerId, x.Amount)));
    }
}
