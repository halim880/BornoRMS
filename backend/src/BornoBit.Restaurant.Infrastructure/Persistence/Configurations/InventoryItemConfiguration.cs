using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code).IsRequired().HasMaxLength(40);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.BanglaName).HasMaxLength(200);
        builder.Property(i => i.ItemType).HasConversion<int>();
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(8);
        builder.Property(i => i.PackNote).HasMaxLength(200);

        builder.Property(i => i.QtyOnHand).HasPrecision(18, 3);
        builder.Property(i => i.ReorderLevel).HasPrecision(18, 3);
        builder.Property(i => i.ReorderQty).HasPrecision(18, 3);
        builder.Property(i => i.PackSize).HasPrecision(18, 3);
        builder.Property(i => i.AvgCost).HasPrecision(18, 2);

        builder.Ignore(i => i.IsLowStock);
        builder.Ignore(i => i.StockValue);

        builder.HasIndex(i => i.Code).IsUnique();
        builder.HasIndex(i => i.InventoryCategoryId);
        builder.HasIndex(i => i.ItemType);

        builder.HasOne<InventoryCategory>()
            .WithMany()
            .HasForeignKey(i => i.InventoryCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(i => i.BaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
