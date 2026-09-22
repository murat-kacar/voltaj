using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface ICustomerService
{
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<Result<CustomerDto>> ConvertToActiveAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>A customer that is not active is kept, with everything that was ever recorded for them, but is left out of the choices for new work.</summary>
    Task<Result<CustomerDto>> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);

    Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyDictionary<Guid, string>>> GetNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    /// <param name="search">Part of the name, email, phone or tax number.</param>
    /// <param name="type">Lead or Active.</param>
    /// <param name="active">True for active customers only, false for the inactive ones, null for both.</param>
    Task<Result<PagedResult<CustomerDto>>> ListAsync(string? search = null, string? type = null, bool? active = null, int? limit = null, int? offset = null, CancellationToken ct = default);
}

public interface ICustomerSiteService
{
    Task<Result<IReadOnlyList<CustomerSiteDto>>> ListAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<CustomerSiteDto>> GetSiteAsync(Guid customerId, Guid siteId, CancellationToken ct = default);
    Task<Result<CustomerAssetDto>> GetAssetAsync(Guid siteId, Guid assetId, CancellationToken ct = default);
    Task<Result<CustomerSiteDto>> CreateSiteAsync(Guid customerId, SaveSiteRequest request, CancellationToken ct = default);
    Task<Result<CustomerSiteDto>> UpdateSiteAsync(Guid customerId, Guid siteId, SaveSiteRequest request, CancellationToken ct = default);
    Task<Result<CustomerAssetDto>> CreateAssetAsync(Guid customerId, Guid siteId, SaveAssetRequest request, CancellationToken ct = default);
    Task<Result<CustomerAssetDto>> UpdateAssetAsync(Guid customerId, Guid siteId, Guid assetId, SaveAssetRequest request, CancellationToken ct = default);
}
