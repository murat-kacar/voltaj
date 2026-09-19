using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltflow.Domain.WorkOrders;

namespace Voltflow.Infrastructure.Persistence.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.SourceQuoteId).IsUnique().HasFilter("\"SourceQuoteId\" IS NOT NULL");
        builder.HasIndex(x => x.AssignedUserId);
        builder.Property(x => x.Number).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Total).HasColumnType("decimal(18,2)");

        builder.OwnsMany(x => x.Items, items =>
        {
            items.WithOwner().HasForeignKey("WorkOrderId");
            items.Property(x => x.Description).IsRequired();
            items.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            items.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        });

        builder.OwnsMany(x => x.TimeEntries, entries =>
        {
            entries.WithOwner().HasForeignKey("WorkOrderId");
            entries.Property(x => x.Notes).HasMaxLength(1000);
        });
    }
}
