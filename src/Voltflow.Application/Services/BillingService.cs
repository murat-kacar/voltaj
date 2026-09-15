using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Projects;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class BillingService : IBillingService
{
    private readonly IBillingRepository _repository;
    public BillingService(IBillingRepository repository) => _repository = repository;

    public async Task<Result<BillingEntryDto>> CreateAsync(Guid projectId, Guid customerId, decimal amount, CancellationToken ct = default)
    {
        if (projectId == Guid.Empty) return Result<BillingEntryDto>.Fail("ProjectId is required.");
        if (customerId == Guid.Empty) return Result<BillingEntryDto>.Fail("CustomerId is required.");
        if (amount <= 0) return Result<BillingEntryDto>.Fail("Amount must be greater than zero.");
        var entry = new BillingEntry(projectId, customerId, amount);
        await _repository.AddAsync(entry, ct);
        return Result<BillingEntryDto>.Ok(new BillingEntryDto(entry.Id, entry.ProjectId, entry.CustomerId, entry.Amount));
    }

    public Task<Result<BillingEntryDto>> CreateAsync(Guid projectId, CreateBillingEntryRequest request, CancellationToken ct = default)
        => CreateAsync(projectId, request.CustomerId, request.Amount, ct);

    public async Task<Result<IReadOnlyList<BillingEntryDto>>> ListAsync(Guid projectId, CancellationToken ct = default)
    {
        var entries = await _repository.GetByProjectAsync(projectId, ct);
        return Result<IReadOnlyList<BillingEntryDto>>.Ok(entries.Select(x => new BillingEntryDto(x.Id, x.ProjectId, x.CustomerId, x.Amount)).ToList());
    }
}
