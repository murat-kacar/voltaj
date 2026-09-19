using Voltflow.Domain.Customers;

namespace Voltflow.Application.Interfaces;

/// <summary>The addresses of a customer and the devices installed at them. The methods that change data do not save; the service commits.</summary>
public interface ICustomerSiteRepository
{
    Task<IReadOnlyList<CustomerSite>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerAsset>> ListAssetsAsync(IReadOnlyCollection<Guid> siteIds, CancellationToken ct = default);

    /// <summary>The site, only when it belongs to that customer.</summary>
    Task<CustomerSite?> GetSiteAsync(Guid customerId, Guid siteId, CancellationToken ct = default);

    /// <summary>The device, only when it stands at that site.</summary>
    Task<CustomerAsset?> GetAssetAsync(Guid siteId, Guid assetId, CancellationToken ct = default);

    /// <summary>Whether a device with this serial number exists, other than the one being changed.</summary>
    Task<bool> SerialNumberExistsAsync(string serialNumber, Guid? exceptAssetId, CancellationToken ct = default);

    void AddSite(CustomerSite site);
    void AddAsset(CustomerAsset asset);
}
