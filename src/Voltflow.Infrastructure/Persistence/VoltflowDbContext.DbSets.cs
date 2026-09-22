using Microsoft.EntityFrameworkCore;
using Voltflow.Domain.Common;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Identity;
using Voltflow.Domain.Inventory;
using Voltflow.Domain.Projects;
using Voltflow.Domain.Finance;
using Voltflow.Domain.Quotes;
using Voltflow.Domain.Sales;
using Voltflow.Domain.WorkOrders;
using Voltflow.Domain.Reminders;
using Voltflow.Domain.Auditing;

namespace Voltflow.Infrastructure.Persistence;

public sealed partial class VoltflowDbContext
{
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
    public DbSet<MaintenanceContract> MaintenanceContracts => Set<MaintenanceContract>();
    public DbSet<CommandRecord> CommandRecords => Set<CommandRecord>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<QuickSale> QuickSales => Set<QuickSale>();
    public DbSet<QuickSaleLine> QuickSaleLines => Set<QuickSaleLine>();
    public DbSet<QuickSalePayment> QuickSalePayments => Set<QuickSalePayment>();
    public DbSet<QuickSaleReturn> QuickSaleReturns => Set<QuickSaleReturn>();
    public DbSet<QuickSaleReturnLine> QuickSaleReturnLines => Set<QuickSaleReturnLine>();
    public DbSet<CashShift> CashShifts => Set<CashShift>();
    public DbSet<DocumentCounter> DocumentCounters => Set<DocumentCounter>();
}
