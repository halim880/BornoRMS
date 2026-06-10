using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementType).HasConversion<int>();
        builder.Property(m => m.QtyBase).HasPrecision(18, 3);
        builder.Property(m => m.UnitCost).HasPrecision(18, 2);
        builder.Property(m => m.Reason).HasMaxLength(500);
        builder.Property(m => m.ReferenceType).HasMaxLength(80);

        builder.HasIndex(m => m.InventoryItemId);
        builder.HasIndex(m => m.OccurredAtUtc);
        builder.HasIndex(m => m.MovementType);

        builder.HasOne<InventoryItem>()
            .WithMany()
            .HasForeignKey(m => m.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
