using Voltflow.Domain.Common;

namespace Voltflow.Domain.Customers;

public sealed class CustomerAsset : Entity
{
    public Guid SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public DateOnly? InstallationDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    private CustomerAsset() { }

    public CustomerAsset(Guid siteId, string name, string serialNumber, DateOnly? installationDate)
    {
        if (siteId == Guid.Empty) throw new ArgumentException("Site id is required.", nameof(siteId));
        SiteId = siteId;
        Name = Guard.NotEmpty(name, nameof(name));
        SerialNumber = serialNumber?.Trim() ?? string.Empty;
        InstallationDate = installationDate;
    }

    public void Update(string name, string serialNumber, DateOnly? installationDate)
    {
        Name = Guard.NotEmpty(name, nameof(name));
        SerialNumber = serialNumber?.Trim() ?? string.Empty;
        InstallationDate = installationDate;
        Touch();
    }

    public void SetInactive() { IsActive = false; Touch(); }
    public void SetActive() { IsActive = true; Touch(); }
}
