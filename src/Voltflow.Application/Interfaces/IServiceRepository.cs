using Voltflow.Application.Common;
using Voltflow.Domain.Services;

namespace Voltflow.Application.Interfaces;

public interface IServiceRepository : IRepository<Service>
{
    Task<PagedResult<Service>> ListPagedAsync(ServiceFilter filter, int limit, int offset, CancellationToken ct = default);
}

/// <summary>V8: filter parameters for listing services.</summary>
public sealed record ServiceFilter(
    string? Search = null,
    string? Status = null,
    Guid? CustomerId = null,
    Guid? AssignedUserId = null);
