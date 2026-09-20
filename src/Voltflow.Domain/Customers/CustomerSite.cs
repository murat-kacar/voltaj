using Voltflow.Domain.Common;

namespace Voltflow.Domain.Customers;

public sealed class CustomerSite : Entity
{
    public Guid CustomerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    [PersonalData] public string Address { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private CustomerSite() { }

    public CustomerSite(Guid customerId, string name, string address)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        CustomerId = customerId;
        Name = Guard.NotEmpty(name, nameof(name));
        Address = Guard.NotEmpty(address, nameof(address));
    }

    public void Update(string name, string address)
    {
        Name = Guard.NotEmpty(name, nameof(name));
        Address = Guard.NotEmpty(address, nameof(address));
        Touch();
    }

    public void SetInactive() { IsActive = false; Touch(); }
    public void SetActive() { IsActive = true; Touch(); }
}
