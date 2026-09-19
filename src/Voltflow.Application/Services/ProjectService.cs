using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Projects;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;

    public ProjectService(IProjectRepository repository, ICommandJournal commandJournal, IOperationContext operationContext)
    {
        _repository = repository;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
    }

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        if (request.CustomerId == Guid.Empty) return Result<ProjectDto>.Fail("CustomerId is required.");
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<ProjectDto>.Fail("Name is required.");
        var project = new Project(request.CustomerId, request.Name.Trim(), request.Budget);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _repository.AddAsync(project, ct);
        return Result<ProjectDto>.Ok(Map(project));
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        return project is null ? Result<ProjectDto>.Fail("Project not found.") : Result<ProjectDto>.Ok(Map(project));
    }

    public async Task<Result<PagedResult<ProjectDto>>> ListAsync(int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var page = await _repository.ListPagedAsync(
            PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        return Result<PagedResult<ProjectDto>>.Ok(page.Map(Map));
    }

    public async Task<Result<ProjectDto>> AddPhaseAsync(Guid id, CreateProjectPhaseRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return Result<ProjectDto>.Fail("Title is required.");
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null) return Result<ProjectDto>.Fail("Project not found.");

        try
        {
            project.AddPhase(request.Title.Trim(), request.PlannedAmount);
            _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
            await _repository.UpdateAsync(project, ct);
            return Result<ProjectDto>.Ok(Map(project));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, "DOMAIN_VALIDATION_FAILED", ct);
            return Result<ProjectDto>.Fail(exception.Message);
        }
    }

    private static ProjectDto Map(Project project) => new(project.Id, project.CustomerId, project.Number, project.Name, project.Budget, project.Phases.Select(x => new ProjectPhaseDto(x.Id, x.Title, x.PlannedAmount)).ToList());
}
