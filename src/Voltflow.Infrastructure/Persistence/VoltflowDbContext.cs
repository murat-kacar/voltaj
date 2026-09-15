using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Common;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Identity;
using Voltflow.Domain.Inventory;
using Voltflow.Domain.Projects;
using Voltflow.Domain.Finance;
using Voltflow.Domain.Quotes;
using Voltflow.Domain.WorkOrders;
using Voltflow.Domain.Reminders;

namespace Voltflow.Infrastructure.Persistence;

public sealed class VoltflowDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentUser? _currentUser;
    private readonly IOperationContext? _operationContext;

    public Task<int> CommitAsync(CancellationToken ct = default) => SaveChangesAsync(ct);


    public VoltflowDbContext(
        DbContextOptions<VoltflowDbContext> options,
        ICurrentUser? currentUser = null,
        IOperationContext? operationContext = null) : base(options)
    {
        _currentUser = currentUser;
        _operationContext = operationContext;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerSite> CustomerSites => Set<CustomerSite>();
    public DbSet<CustomerAsset> CustomerAssets => Set<CustomerAsset>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<MaterialStock> MaterialStocks => Set<MaterialStock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<BillingEntry> BillingEntries => Set<BillingEntry>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<CustomerLedgerEntry> CustomerLedgerEntries => Set<CustomerLedgerEntry>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<PaymentInvoiceAllocation> PaymentInvoiceAllocations => Set<PaymentInvoiceAllocation>();
    public DbSet<ProgressBilling> ProgressBillings => Set<ProgressBilling>();
    public DbSet<ReminderRecord> ReminderRecords => Set<ReminderRecord>();
    public DbSet<ReferenceValue> ReferenceValues => Set<ReferenceValue>();
    public DbSet<ExecutionGuardRecord> ExecutionGuards => Set<ExecutionGuardRecord>();
    public DbSet<OperationTrace> OperationTraces => Set<OperationTrace>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<MaintenanceContract> MaintenanceContracts => Set<MaintenanceContract>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VoltflowDbContext).Assembly);

        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => typeof(Voltflow.Domain.Common.Entity).IsAssignableFrom(type.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(Voltflow.Domain.Common.Entity.Version))
                .IsConcurrencyToken()
                .HasDefaultValue(1L);
        }
    }

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

        foreach (var entry in ChangeTracker.Entries<Voltflow.Domain.Common.Entity>().ToList())
        {
            if (entry.Entity is AuditEvent or OperationTrace or OutboxMessage or ExecutionGuardRecord) continue;

            if (entry.State == EntityState.Added)
                entry.Entity.SetCreatedContext(userId, endpoint, operationId);
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdatedContext(userId, endpoint, operationId);
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
