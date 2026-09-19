using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltflow.Domain.Finance;
using Voltflow.Domain.Projects;

namespace Voltflow.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Budget).HasColumnType("decimal(18,2)");

        builder.OwnsMany(x => x.Phases, phases =>
        {
            phases.WithOwner().HasForeignKey("ProjectId");
            phases.Property(x => x.Title).IsRequired();
            phases.Property(x => x.PlannedAmount).HasColumnType("decimal(18,2)");
        });
    }
}

public class BillingEntryConfiguration : IEntityTypeConfiguration<BillingEntry>
{
    public void Configure(EntityTypeBuilder<BillingEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
    }
}

public class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaymentMethod).IsRequired();
    }
}

public class CustomerLedgerEntryConfiguration : IEntityTypeConfiguration<CustomerLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CustomerLedgerEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Direction).IsRequired();
    }
}

public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AppliedDepositAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Type).HasConversion<string>().IsRequired();
    }
}

public class PaymentInvoiceAllocationConfiguration : IEntityTypeConfiguration<PaymentInvoiceAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentInvoiceAllocation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.PaymentId, x.InvoiceId }).IsUnique();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
    }
}

public class ProgressBillingConfiguration : IEntityTypeConfiguration<ProgressBilling>
{
    public void Configure(EntityTypeBuilder<ProgressBilling> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProjectId, x.BillingNumber }).IsUnique();
        builder.Property(x => x.RequestedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ApprovedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DeductionAmount).HasColumnType("decimal(18,2)");
    }
}
