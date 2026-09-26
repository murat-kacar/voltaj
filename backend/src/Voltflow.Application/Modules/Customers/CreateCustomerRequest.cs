namespace Voltflow.Application.Dtos;

/// <param name="Email">Optional; a customer met at the door often has only a phone.</param>
public sealed record CreateCustomerRequest(string FullName, string? Email, string Phone, string? TaxNumber = null);

// ---- addresses (sites) and the devices installed at them ----------------------------------------------------