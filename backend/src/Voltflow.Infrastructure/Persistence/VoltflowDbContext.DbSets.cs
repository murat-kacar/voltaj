using Microsoft.EntityFrameworkCore;
using Voltflow.Domain.Common;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Identity;
using Voltflow.Domain.Inventory;
using Voltflow.Domain.Finance;
using Voltflow.Domain.Services;
using Voltflow.Domain.Sales;
using Voltflow.Domain.Reminders;
using Voltflow.Domain.Auditing;

namespace Voltflow.Infrastructure.Persistence;

public sealed partial class VoltflowDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerSite> CustomerSites => Set<CustomerSite>();
    public DbSet<CustomerAsset> CustomerAssets => Set<CustomerAsset>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<MaterialStock> MaterialStocks => Set<MaterialStock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
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
    public DbSet<CommandRecord> CommandRecords => Set<CommandRecord>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DocumentCounter> DocumentCounters => Set<DocumentCounter>();
}
