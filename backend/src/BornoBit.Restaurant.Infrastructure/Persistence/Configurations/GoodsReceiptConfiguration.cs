using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("GoodsReceipts");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.GrnNumber).IsRequired().HasMaxLength(40);
        builder.Property(g => g.InvoiceNo).HasMaxLength(80);
        builder.Property(g => g.Currency).IsRequired().HasMaxLength(8);
        builder.Property(g => g.Notes).HasMaxLength(1000);
        builder.Property(g => g.Status).HasConversion<int>();

        builder.Ignore(g => g.Subtotal);

        builder.HasIndex(g => g.GrnNumber).IsUnique();
        builder.HasIndex(g => g.SupplierId);
        builder.HasIndex(g => g.Status);
        builder.HasIndex(g => g.PurchaseOrderId);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(g => g.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PurchaseOrder>()
            .WithMany()
            .HasForeignKey(g => g.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Lines)
            .WithOne()
            .HasForeignKey(l => l.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(GoodsReceipt.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.ToTable("GoodsReceiptLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Qty).HasPrecision(18, 3);
        builder.Property(l => l.QtyBase).HasPrecision(18, 3);
        builder.Property(l => l.UnitCost).HasPrecision(18, 2);

        builder.Ignore(l => l.LineTotal);

        builder.HasIndex(l => l.GoodsReceiptId);

        builder.HasOne<InventoryItem>()
            .WithMany()
            .HasForeignKey(l => l.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
