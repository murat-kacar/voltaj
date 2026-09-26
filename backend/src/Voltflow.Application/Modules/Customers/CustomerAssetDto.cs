namespace Voltflow.Application.Dtos;

public sealed record CustomerAssetDto(Guid Id, Guid SiteId, string Name, string SerialNumber, DateOnly? InstallationDate, bool IsActive);