using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class StockProjectionConfiguration : IEntityTypeConfiguration<StockProjection>
{
    public void Configure(EntityTypeBuilder<StockProjection> builder)
    {
        builder.ToTable("StockProjections");

        // 1:1 with the item — the item id IS the primary key.
        builder.HasKey(p => p.InventoryItemId);
        builder.Property(p => p.InventoryItemId).ValueGeneratedNever();
        builder.Property(p => p.CurrentStock).HasPrecision(18, 3);

        builder.HasOne<InventoryItem>()
            .WithOne()
            .HasForeignKey<StockProjection>(p => p.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
