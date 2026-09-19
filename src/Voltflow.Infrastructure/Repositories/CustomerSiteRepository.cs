using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class CustomerSiteRepository : ICustomerSiteRepository
{
    private readonly VoltflowDbContext _dbContext;

    public CustomerSiteRepository(VoltflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CustomerSite>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
        => await _dbContext.CustomerSites.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CustomerAsset>> ListAssetsAsync(IReadOnlyCollection<Guid> siteIds, CancellationToken ct = default)
    {
        if (siteIds.Count == 0) return [];
        var wanted = siteIds.ToList();
        return await _dbContext.CustomerAssets.AsNoTracking()
            .Where(x => wanted.Contains(x.SiteId))
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public Task<CustomerSite?> GetSiteAsync(Guid customerId, Guid siteId, CancellationToken ct = default)
        => _dbContext.CustomerSites.FirstOrDefaultAsync(x => x.Id == siteId && x.CustomerId == customerId, ct);

    public Task<CustomerAsset?> GetAssetAsync(Guid siteId, Guid assetId, CancellationToken ct = default)
        => _dbContext.CustomerAssets.FirstOrDefaultAsync(x => x.Id == assetId && x.SiteId == siteId, ct);

    public Task<bool> SerialNumberExistsAsync(string serialNumber, Guid? exceptAssetId, CancellationToken ct = default)
        => _dbContext.CustomerAssets.AnyAsync(x => x.SerialNumber == serialNumber && (exceptAssetId == null || x.Id != exceptAssetId), ct);

    public void AddSite(CustomerSite site) => _dbContext.CustomerSites.Add(site);

    public void AddAsset(CustomerAsset asset) => _dbContext.CustomerAssets.Add(asset);
}
