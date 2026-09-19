namespace Voltflow.Application.Dtos;

/// <param name="Email">Optional; a customer met at the door often has only a phone.</param>
public sealed record CreateCustomerRequest(string FullName, string? Email, string Phone, string? TaxNumber = null);

public sealed record UpdateCustomerRequest(string FullName, string? Email, string Phone, string? TaxNumber);

public sealed record CustomerDto(Guid Id, string FullName, string Email, string Phone, string TaxNumber, string Type, bool IsActive, DateTime CreatedAt);

// ---- addresses (sites) and the devices installed at them ----------------------------------------------------

public sealed record SaveSiteRequest(string Name, string Address, bool IsActive = true);

public sealed record SaveAssetRequest(string Name, string? SerialNumber, DateOnly? InstallationDate, bool IsActive = true);

public sealed record CustomerAssetDto(Guid Id, Guid SiteId, string Name, string SerialNumber, DateOnly? InstallationDate, bool IsActive);

public sealed record CustomerSiteDto(Guid Id, Guid CustomerId, string Name, string Address, bool IsActive, IReadOnlyList<CustomerAssetDto> Assets);
