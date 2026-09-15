using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltflow.Domain.Quotes;

namespace Voltflow.Infrastructure.Persistence.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Total).HasColumnType("decimal(18,2)");

        builder.OwnsMany(x => x.Items, items =>
        {
            items.WithOwner().HasForeignKey("QuoteId");
            items.Property(x => x.Description).IsRequired();
            items.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            items.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        });
    }
}
