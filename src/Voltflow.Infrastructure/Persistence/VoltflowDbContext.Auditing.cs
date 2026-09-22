using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Voltflow.Domain.Auditing;

namespace Voltflow.Infrastructure.Persistence;

public sealed partial class VoltflowDbContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampCreatedBy();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampCreatedBy();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampCreatedBy()
    {
        var userId = _operationContext?.UserId ?? _currentUser?.UserId;
        var endpoint = _operationContext?.Endpoint ?? "unknown";
        var operationId = _operationContext?.OperationId ?? Guid.Empty;
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<Voltflow.Domain.Common.Entity>().ToList())
        {
            if (entry.Entity is AuditEvent or OperationTrace or OutboxMessage or ExecutionGuardRecord or CommandRecord) continue;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedContext(userId, endpoint, operationId);
                entry.Entity.SetCreatedAt(utcNow);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdatedContext(userId, endpoint, operationId, utcNow);
                entry.Entity.IncrementVersion();
            }

            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                AddAuditEvent(entry, userId, operationId, endpoint);
        }

        foreach (var audit in ChangeTracker.Entries<AuditEvent>()
                     .Where(entry => entry.State == EntityState.Added)
                 .Select(entry => entry.Entity)
                 .ToList())
        {
            var payload = JsonSerializer.Serialize(new
            {
                audit.OperationId,
                audit.UserId,
                audit.EventType,
                audit.EntityName,
                audit.EntityId,
                audit.SourceEndpoint,
                audit.SourceScreen,
                audit.SourceAction,
                audit.BeforeJson,
                audit.AfterJson,
                audit.ChangedFieldsJson
            });
            OutboxMessages.Add(new OutboxMessage("AuditEventRecorded", payload));
        }
    }

    private void AddAuditEvent(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Voltflow.Domain.Common.Entity> entry, Guid? userId, Guid operationId, string endpoint)
    {
        if (operationId == Guid.Empty) return;
        var before = entry.Properties
            .Where(property => property.Metadata.Name is not nameof(Voltflow.Domain.Common.Entity.CreatedByUserId)
                and not nameof(Voltflow.Domain.Common.Entity.UpdatedByUserId)
                and not nameof(Voltflow.Domain.Common.Entity.CreatedByEndpoint)
                and not nameof(Voltflow.Domain.Common.Entity.UpdatedByEndpoint)
                and not nameof(Voltflow.Domain.Common.Entity.CreatedInOperationId)
                and not nameof(Voltflow.Domain.Common.Entity.UpdatedInOperationId))
            .Where(property => entry.State != EntityState.Added && (property.IsModified || entry.State == EntityState.Deleted))
            .ToDictionary(property => property.Metadata.Name, property => Redact(property.Metadata.Name, property.OriginalValue));
        var after = entry.Properties
            .Where(property => property.Metadata.Name is not nameof(Voltflow.Domain.Common.Entity.CreatedByUserId)
                and not nameof(Voltflow.Domain.Common.Entity.UpdatedByUserId)
                and not nameof(Voltflow.Domain.Common.Entity.CreatedByEndpoint)
                and not nameof(Voltflow.Domain.Common.Entity.UpdatedByEndpoint)
                and not nameof(Voltflow.Domain.Common.Entity.CreatedInOperationId)
                and not nameof(Voltflow.Domain.Common.Entity.UpdatedInOperationId))
            .Where(property => entry.State != EntityState.Deleted)
            .ToDictionary(property => property.Metadata.Name, property => Redact(property.Metadata.Name, property.CurrentValue));
        var changedFields = entry.Properties
            .Where(property => entry.State == EntityState.Added || entry.State == EntityState.Deleted || property.IsModified)
            .Select(property => property.Metadata.Name)
            .ToArray();
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        var beforeJson = JsonSerializer.Serialize(before, options);
        var afterJson = JsonSerializer.Serialize(after, options);
        var changedFieldsJson = JsonSerializer.Serialize(changedFields, options);
        var eventType = entry.State == EntityState.Added ? "CREATED" : entry.State == EntityState.Deleted ? "DELETED" : "UPDATED";
        AuditEvents.Add(new AuditEvent(
            operationId,
            _operationContext?.ParentOperationId,
            userId,
            eventType,
            entry.Entity.GetType().Name,
            entry.Entity.Id,
            endpoint,
            _operationContext?.Screen,
            _operationContext?.Action,
            beforeJson,
            afterJson,
            changedFieldsJson));
    }

    private static object? Redact(string name, object? value)
        => name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("email", StringComparison.OrdinalIgnoreCase)
            || name.Contains("phone", StringComparison.OrdinalIgnoreCase)
            || name.Contains("tax", StringComparison.OrdinalIgnoreCase)
            || name.Contains("address", StringComparison.OrdinalIgnoreCase)
            ? "[REDACTED]" : value;
}
