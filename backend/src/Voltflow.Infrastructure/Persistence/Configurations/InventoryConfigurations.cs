using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltflow.Domain.Inventory;

namespace Voltflow.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Type).IsRequired();
    }
}

public class MaterialStockConfiguration : IEntityTypeConfiguration<MaterialStock>
{
    public void Configure(EntityTypeBuilder<MaterialStock> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialCode).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.QuantityOnHand).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReservedQuantity).HasColumnType("decimal(18,2)");
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialCode).IsRequired();
        builder.Property(x => x.QuantityDelta).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Direction).HasConversion<string>().IsRequired();
        builder.Property(x => x.PreviousQuantity).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NewQuantity).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Reason).IsRequired();
    }
}
