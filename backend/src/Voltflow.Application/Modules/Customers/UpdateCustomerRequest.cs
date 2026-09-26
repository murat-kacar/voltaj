namespace Voltflow.Application.Dtos;

public sealed record UpdateCustomerRequest(string FullName, string? Email, string Phone, string? TaxNumber);