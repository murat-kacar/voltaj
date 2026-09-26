using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltflow.Domain.Customers;

namespace Voltflow.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).IsRequired();
        builder.Property(x => x.Email).IsRequired();
        builder.Property(x => x.Phone).IsRequired();
        builder.Property(x => x.TaxNumber).IsRequired();
        builder.HasIndex(x => x.TaxNumber).IsUnique().HasFilter("\"TaxNumber\" <> ''");
    }
}

public class CustomerSiteConfiguration : IEntityTypeConfiguration<CustomerSite>
{
    public void Configure(EntityTypeBuilder<CustomerSite> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Address).IsRequired();
    }
}

public class CustomerAssetConfiguration : IEntityTypeConfiguration<CustomerAsset>
{
    public void Configure(EntityTypeBuilder<CustomerAsset> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired();
        builder.HasIndex(x => x.SerialNumber).IsUnique().HasFilter("\"SerialNumber\" <> ''");
    }
}
