using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IAuditLogService
{
    Task<Result<AuditLogDto>> CreateAsync(CreateAuditLogRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AuditLogDto>>> ListRecentAsync(int limit = 50, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AuditLogDto>>> ListByEntityAsync(string entityName, string entityId, CancellationToken ct = default);
}
