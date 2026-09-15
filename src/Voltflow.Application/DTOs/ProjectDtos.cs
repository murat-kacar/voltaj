namespace Voltflow.Application.Dtos;

public sealed record CreateProjectRequest(Guid CustomerId, string Name, decimal Budget);
public sealed record CreateProjectPhaseRequest(string Title, decimal PlannedAmount);
public sealed record ProjectDto(Guid Id, Guid CustomerId, string Number, string Name, decimal Budget, IReadOnlyCollection<ProjectPhaseDto> Phases);
public sealed record ProjectPhaseDto(Guid Id, string Title, decimal PlannedAmount);
