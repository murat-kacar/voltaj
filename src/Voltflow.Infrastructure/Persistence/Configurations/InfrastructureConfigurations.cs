using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltflow.Domain.Common;
using Voltflow.Domain.Identity;
using Voltflow.Domain.Reminders;

namespace Voltflow.Infrastructure.Persistence.Configurations;

public class ReminderRecordConfiguration : IEntityTypeConfiguration<ReminderRecord>
{
    public void Configure(EntityTypeBuilder<ReminderRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.EntityName).IsRequired();
        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.State).HasConversion<string>().IsRequired();
    }
}

public class ReferenceValueConfiguration : IEntityTypeConfiguration<ReferenceValue>
{
    public void Configure(EntityTypeBuilder<ReferenceValue> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Code).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.HasIndex(x => new { x.Type, x.Code }).IsUnique();
    }
}

public class ExecutionGuardRecordConfiguration : IEntityTypeConfiguration<ExecutionGuardRecord>
{
    public void Configure(EntityTypeBuilder<ExecutionGuardRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Scope).IsRequired();
        builder.Property(x => x.IdempotencyKey).IsRequired();
        builder.Property(x => x.RequestHash).IsRequired();
        builder.Property(x => x.State).HasConversion<string>().IsRequired();
        builder.HasIndex(x => new { x.Scope, x.IdempotencyKey }).IsUnique();
    }
}

public class OperationTraceConfiguration : IEntityTypeConfiguration<OperationTrace>
{
    public void Configure(EntityTypeBuilder<OperationTrace> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OperationId).IsUnique();
        builder.Property(x => x.Endpoint).IsRequired();
        builder.Property(x => x.Outcome).IsRequired();
    }
}

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OperationId);
        builder.Property(x => x.EventType).IsRequired();
        builder.Property(x => x.EntityName).IsRequired();
        builder.Property(x => x.SourceEndpoint).IsRequired();
        builder.Property(x => x.BeforeJson).IsRequired();
        builder.Property(x => x.AfterJson).IsRequired();
        builder.Property(x => x.ChangedFieldsJson).IsRequired();
    }
}

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).IsRequired();
        builder.Property(x => x.State).HasConversion<string>().IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
    }
}
