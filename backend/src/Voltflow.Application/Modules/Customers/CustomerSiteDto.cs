namespace Voltflow.Application.Dtos;

public sealed record CustomerSiteDto(Guid Id, Guid CustomerId, string Name, string Address, bool IsActive, IReadOnlyList<CustomerAssetDto> Assets);