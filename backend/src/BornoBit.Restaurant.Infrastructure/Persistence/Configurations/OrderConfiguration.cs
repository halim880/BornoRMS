using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(40);
        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.OrderType).HasConversion<int>();
        builder.Property(o => o.Status).HasConversion<int>();
        builder.Property(o => o.Currency).IsRequired().HasMaxLength(8);
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.CancellationReason).HasMaxLength(1000);

        builder.Property(o => o.DiscountAmount).HasPrecision(18, 2);
        builder.Property(o => o.DiscountPercent).HasPrecision(5, 2);
        builder.Property(o => o.DiscountReason).HasMaxLength(500);
        builder.Property(o => o.PaymentMethod).HasConversion<int>();
        builder.Property(o => o.AmountTendered).HasPrecision(18, 2);
        builder.Property(o => o.ChangeGiven).HasPrecision(18, 2);
        builder.Property(o => o.RoundingAdjustment).HasPrecision(18, 2);

        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.Subtotal);
        builder.Ignore(o => o.GrandTotal);

        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.Status);

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Order.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RestaurantTable>()
            .WithMany()
            .HasForeignKey(o => o.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrderId).IsRequired();
        builder.Property(l => l.MenuItemId).IsRequired();
        builder.Property(l => l.Code).IsRequired().HasMaxLength(40);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(200);
        builder.Property(l => l.UnitPriceSnapshot).HasPrecision(18, 2);
        builder.Property(l => l.Currency).IsRequired().HasMaxLength(8);
        builder.Property(l => l.Quantity).IsRequired();

        builder.Ignore(l => l.LineTotal);

        builder.HasIndex(l => l.OrderId);
    }
}
