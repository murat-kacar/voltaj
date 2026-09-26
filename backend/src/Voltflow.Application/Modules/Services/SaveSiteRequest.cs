namespace Voltflow.Application.Dtos;

public sealed record SaveSiteRequest(string Name, string Address, bool IsActive = true);