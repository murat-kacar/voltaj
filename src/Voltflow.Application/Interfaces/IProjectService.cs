using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IProjectService
{
    Task<Result<ProjectDto>> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<Result<ProjectDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<ProjectDto>> AddPhaseAsync(Guid id, CreateProjectPhaseRequest request, CancellationToken ct = default);
}
