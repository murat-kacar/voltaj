using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltflow.Domain.Services;

namespace Voltflow.Infrastructure.Persistence.Configurations;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Number).HasMaxLength(32).IsRequired();
        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(2000);
        
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(s => s.RejectionReason).HasMaxLength(500);

        builder.Property(s => s.RequiredDepositPercentage).HasPrecision(5, 2);
        builder.Property(s => s.DepositPaidAmount).HasPrecision(18, 2);
        
        builder.Property(s => s.TotalBilled).HasPrecision(18, 2);
        builder.Property(s => s.CurrentTotal).HasPrecision(18, 2);
        
        builder.Ignore(s => s.RemainingLimit);
        builder.Ignore(s => s.RequiredDepositAmount);

        // One-to-many relationship mapping for Items
        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
            
        var navigation = builder.Metadata.FindNavigation(nameof(Service.Items));
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> builder)
    {
        builder.ToTable("ServiceItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Unit).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Kind).HasMaxLength(50).IsRequired();
        builder.Property(i => i.AuditNote).HasMaxLength(500).IsRequired();

        builder.Property(i => i.Quantity).HasPrecision(18, 4);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 4);
        builder.Property(i => i.VatRate).HasPrecision(5, 2);

        builder.Ignore(i => i.LineTotal);
        builder.Ignore(i => i.VatAmount);
        builder.Ignore(i => i.IsActive);
    }
}
