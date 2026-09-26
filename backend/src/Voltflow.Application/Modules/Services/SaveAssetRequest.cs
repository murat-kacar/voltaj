namespace Voltflow.Application.Dtos;

public sealed record SaveAssetRequest(string Name, string? SerialNumber, DateOnly? InstallationDate, bool IsActive = true);