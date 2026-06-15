using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PoNumber).IsRequired().HasMaxLength(40);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(8);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.Status).HasConversion<int>();

        builder.Ignore(p => p.Subtotal);

        builder.HasIndex(p => p.PoNumber).IsUnique();
        builder.HasIndex(p => p.SupplierId);
        builder.HasIndex(p => p.Status);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Lines)
            .WithOne()
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(PurchaseOrder.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.QtyOrdered).HasPrecision(18, 3);
        builder.Property(l => l.QtyOrderedBase).HasPrecision(18, 3);
        builder.Property(l => l.QtyReceivedBase).HasPrecision(18, 3);
        builder.Property(l => l.UnitCost).HasPrecision(18, 2);

        builder.Ignore(l => l.LineTotal);
        builder.Ignore(l => l.OutstandingBase);
        builder.Ignore(l => l.IsFullyReceived);

        builder.HasIndex(l => l.PurchaseOrderId);

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
