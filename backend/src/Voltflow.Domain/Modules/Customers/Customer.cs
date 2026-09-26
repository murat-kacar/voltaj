using Voltflow.Domain.Common;

namespace Voltflow.Domain.Customers;

public sealed class Customer : Entity
{
    [PersonalData] public string FullName { get; private set; } = string.Empty;

    /// <summary>Optional: a customer met at the door often has a phone and no email.</summary>
    [PersonalData] public string Email { get; private set; } = string.Empty;

    [PersonalData] public string Phone { get; private set; } = string.Empty;
    [PersonalData] public string TaxNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public CustomerType Type { get; private set; } = CustomerType.Lead;

    private Customer() { }

    public Customer(string fullName, string email, string phone)
        : this(fullName, email, phone, string.Empty)
    {
    }

    public Customer(string fullName, string? email, string phone, string? taxNumber)
    {
        FullName = Guard.NotEmpty(fullName, nameof(fullName)).Trim();
        Email = email?.Trim() ?? string.Empty;
        Phone = Guard.NotEmpty(phone, nameof(phone)).Trim();
        TaxNumber = taxNumber?.Trim() ?? string.Empty;
        Type = CustomerType.Lead;
    }

    public void ConvertToActive()
    {
        if (Type == CustomerType.Active) throw new InvalidOperationException("Customer is already active.");
        Type = CustomerType.Active;
        Touch();
    }

    public void Update(string fullName, string email, string phone) => Update(fullName, email, phone, TaxNumber);

    public void Update(string fullName, string? email, string phone, string? taxNumber)
    {
        // checked first, so a rejected update leaves the customer as it was
        var cleanName = Guard.NotEmpty(fullName, nameof(fullName)).Trim();
        var cleanPhone = Guard.NotEmpty(phone, nameof(phone)).Trim();

        FullName = cleanName;
        Email = email?.Trim() ?? string.Empty;
        Phone = cleanPhone;
        TaxNumber = taxNumber?.Trim() ?? string.Empty;
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
