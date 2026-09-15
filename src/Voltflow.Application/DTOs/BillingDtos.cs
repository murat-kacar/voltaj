namespace Voltflow.Application.Dtos;

public sealed record CreateBillingEntryRequest(Guid CustomerId, decimal Amount);
public sealed record BillingEntryDto(Guid Id, Guid ProjectId, Guid CustomerId, decimal Amount);

