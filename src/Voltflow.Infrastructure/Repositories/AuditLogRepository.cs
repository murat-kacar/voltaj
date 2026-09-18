using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Auditing;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(VoltflowDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AuditLog>> ListRecentAsync(int limit = 50, CancellationToken ct = default)
    {
        return await DbContext.Set<AuditLog>()
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> ListByEntityAsync(string entityName, string entityId, CancellationToken ct = default)
    {
        return await DbContext.Set<AuditLog>()
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(ct);
    }
}
