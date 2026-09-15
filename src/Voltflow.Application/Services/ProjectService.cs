using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Projects;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;
    public ProjectService(IProjectRepository repository) => _repository = repository;

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        if (request.CustomerId == Guid.Empty) return Result<ProjectDto>.Fail("CustomerId is required.");
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<ProjectDto>.Fail("Name is required.");
        var project = new Project(request.CustomerId, request.Name.Trim(), request.Budget);
        await _repository.AddAsync(project, ct);
        return Result<ProjectDto>.Ok(Map(project));
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        return project is null ? Result<ProjectDto>.Fail("Project not found.") : Result<ProjectDto>.Ok(Map(project));
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> ListAsync(CancellationToken ct = default)
    {
        var projects = await _repository.ListAsync(ct);
        return Result<IReadOnlyList<ProjectDto>>.Ok(projects.Select(Map).ToList());
    }

    public async Task<Result<ProjectDto>> AddPhaseAsync(Guid id, CreateProjectPhaseRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return Result<ProjectDto>.Fail("Title is required.");
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null) return Result<ProjectDto>.Fail("Project not found.");

        try
        {
            project.AddPhase(request.Title.Trim(), request.PlannedAmount);
            await _repository.UpdateAsync(project, ct);
            return Result<ProjectDto>.Ok(Map(project));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Result<ProjectDto>.Fail(exception.Message);
        }
    }

    private static ProjectDto Map(Project project) => new(project.Id, project.CustomerId, project.Number, project.Name, project.Budget, project.Phases.Select(x => new ProjectPhaseDto(x.Id, x.Title, x.PlannedAmount)).ToList());
}
