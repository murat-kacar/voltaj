namespace Voltflow.Application.Dtos;

public sealed record CreateCustomerRequest(string FullName, string Email, string Phone, string? TaxNumber = null);

public sealed record CustomerDto(Guid Id, string FullName, string Email, string Phone, string TaxNumber, string Type, bool IsActive, DateTime CreatedAt);

