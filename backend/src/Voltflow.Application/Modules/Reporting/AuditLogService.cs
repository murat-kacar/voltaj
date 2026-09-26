using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Auditing;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<AuditLogDto>> CreateAsync(CreateAuditLogRequest request, CancellationToken ct = default)
    {
        if (request.UserId == Guid.Empty) return Result<AuditLogDto>.Fail("UserId is required.");
        if (string.IsNullOrWhiteSpace(request.Action)) return Result<AuditLogDto>.Fail("Action is required.");
        if (string.IsNullOrWhiteSpace(request.EntityName)) return Result<AuditLogDto>.Fail("EntityName is required.");
        if (string.IsNullOrWhiteSpace(request.EntityId)) return Result<AuditLogDto>.Fail("EntityId is required.");

        var log = new AuditLog(request.UserId, request.Action, request.EntityName, request.EntityId, request.Details, DateTime.UtcNow);
        await _repository.AddAsync(log, ct);
        
        return Result<AuditLogDto>.Ok(Map(log));
    }

    public async Task<Result<IReadOnlyList<AuditLogDto>>> ListRecentAsync(int limit = 50, CancellationToken ct = default)
    {
        var logs = await _repository.ListRecentAsync(limit, ct);
        return Result<IReadOnlyList<AuditLogDto>>.Ok(logs.Select(Map).ToList());
    }

    public async Task<Result<IReadOnlyList<AuditLogDto>>> ListByEntityAsync(string entityName, string entityId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(entityId))
            return Result<IReadOnlyList<AuditLogDto>>.Fail("EntityName and EntityId are required.");

        var logs = await _repository.ListByEntityAsync(entityName, entityId, ct);
        return Result<IReadOnlyList<AuditLogDto>>.Ok(logs.Select(Map).ToList());
    }

    private static AuditLogDto Map(AuditLog log) =>
        new(log.Id, log.UserId, log.Action, log.EntityName, log.EntityId, log.Details, log.Timestamp);
}
