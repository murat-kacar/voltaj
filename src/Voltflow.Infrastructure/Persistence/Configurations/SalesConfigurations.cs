using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Inventory;
using Voltflow.Domain.Sales;

namespace Voltflow.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Unit).IsRequired();
        builder.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.VatRate).HasColumnType("decimal(5,2)");
        builder.HasIndex(x => x.Code).IsUnique();
        // Postgres treats NULLs as distinct, so any number of products without a barcode can coexist.
        builder.HasIndex(x => x.Barcode).IsUnique();
    }
}

public class CashShiftConfiguration : IEntityTypeConfiguration<CashShift>
{
    public void Configure(EntityTypeBuilder<CashShift> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CashierName).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().IsRequired();
        builder.Property(x => x.OpeningCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CountedCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ExpectedCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CashDifference).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.OpenedAt);
        // One open shift per cashier. The service checks first; this index is the safety net for two requests at once.
        builder.HasIndex(x => x.CashierUserId).IsUnique().HasFilter("\"Status\" = 'Open'").HasDatabaseName("IX_CashShifts_OneOpenPerCashier");
    }
}

public class DocumentCounterConfiguration : IEntityTypeConfiguration<DocumentCounter>
{
    public void Configure(EntityTypeBuilder<DocumentCounter> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

public class QuickSaleConfiguration : IEntityTypeConfiguration<QuickSale>
{
    public void Configure(EntityTypeBuilder<QuickSale> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SaleNumber).IsRequired();
        builder.Property(x => x.CashierName).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().IsRequired();
        builder.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LineDiscountTotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReceiptDiscount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.VatTotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CashTendered).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ChangeGiven).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.SaleNumber).IsUnique();
        builder.HasIndex(x => x.SoldAt);
        builder.HasIndex(x => x.ShiftId);
        builder.HasIndex(x => x.CustomerId);

        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.QuickSaleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.QuickSaleId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);

        // A sale belongs to a shift and may point at a customer; neither can be deleted while sales reference them.
        builder.HasOne<CashShift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QuickSaleLineConfiguration : IEntityTypeConfiguration<QuickSaleLine>
{
    public void Configure(EntityTypeBuilder<QuickSaleLine> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Unit).IsRequired();
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.VatRate).HasColumnType("decimal(5,2)");
        builder.Property(x => x.LineDiscount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReceiptDiscountShare).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.VatAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReturnedQuantity).HasColumnType("decimal(18,2)");
        builder.Ignore(x => x.RemainingQuantity);
        builder.HasIndex(x => x.QuickSaleId);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QuickSalePaymentConfiguration : IEntityTypeConfiguration<QuickSalePayment>
{
    public void Configure(EntityTypeBuilder<QuickSalePayment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Method).HasConversion<string>().IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Tendered).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.QuickSaleId);
    }
}

public class QuickSaleReturnConfiguration : IEntityTypeConfiguration<QuickSaleReturn>
{
    public void Configure(EntityTypeBuilder<QuickSaleReturn> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReturnNumber).IsRequired();
        builder.Property(x => x.SaleNumber).IsRequired();
        builder.Property(x => x.CashierName).IsRequired();
        builder.Property(x => x.Reason).IsRequired();
        builder.Property(x => x.RefundMethod).HasConversion<string>().IsRequired();
        builder.Property(x => x.RefundTotal).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.ReturnNumber).IsUnique();
        builder.HasIndex(x => x.QuickSaleId);
        builder.HasIndex(x => x.ShiftId);

        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.QuickSaleReturnId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<QuickSale>().WithMany().HasForeignKey(x => x.QuickSaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashShift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QuickSaleReturnLineConfiguration : IEntityTypeConfiguration<QuickSaleReturnLine>
{
    public void Configure(EntityTypeBuilder<QuickSaleReturnLine> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RefundAmount).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.QuickSaleReturnId);
        builder.HasOne<QuickSaleLine>().WithMany().HasForeignKey(x => x.QuickSaleLineId).OnDelete(DeleteBehavior.Restrict);
    }
}
