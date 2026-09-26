using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voltflow.Domain.Auditing;

namespace Voltflow.Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> ListRecentAsync(int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> ListByEntityAsync(string entityName, string entityId, CancellationToken ct = default);
}
