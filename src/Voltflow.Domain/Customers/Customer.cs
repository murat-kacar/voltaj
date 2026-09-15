using Voltflow.Domain.Common;

namespace Voltflow.Domain.Customers;

public sealed class Customer : Entity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string TaxNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public CustomerType Type { get; private set; } = CustomerType.Lead;

    private Customer() { }

    public Customer(string fullName, string email, string phone)
        : this(fullName, email, phone, string.Empty)
    {
    }

    public Customer(string fullName, string email, string phone, string taxNumber)
    {
        FullName = Guard.NotEmpty(fullName, nameof(fullName));
        Email = Guard.NotEmpty(email, nameof(email));
        Phone = Guard.NotEmpty(phone, nameof(phone));
        TaxNumber = taxNumber?.Trim() ?? string.Empty;
        Type = CustomerType.Lead;
    }

    public void ConvertToActive()
    {
        if (Type == CustomerType.Active) throw new InvalidOperationException("Customer is already active.");
        Type = CustomerType.Active;
        Touch();
    }

    public void Update(string fullName, string email, string phone)
    {
        FullName = Guard.NotEmpty(fullName, nameof(fullName));
        Email = Guard.NotEmpty(email, nameof(email));
        Phone = Guard.NotEmpty(phone, nameof(phone));
        Touch();
    }

    public void SetInactive() { IsActive = false; Touch(); }
    public void SetActive() { IsActive = true; Touch(); }
}

public enum CustomerType
{
    Lead,
    Active
}
