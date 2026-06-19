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
        builder.Property(o => o.KitchenNotes).HasMaxLength(2000);
        builder.Property(o => o.IsPriority).HasDefaultValue(false);

        builder.Property(o => o.DiscountAmount).HasPrecision(18, 2);
        builder.Property(o => o.DiscountPercent).HasPrecision(5, 2);
        builder.Property(o => o.DiscountReason).HasMaxLength(500);
        builder.Property(o => o.TaxAmount).HasPrecision(18, 2);
        builder.Property(o => o.ServiceChargeAmount).HasPrecision(18, 2);
        builder.Property(o => o.TipAmount).HasPrecision(18, 2);
        builder.Property(o => o.DeliveryChargeAmount).HasPrecision(18, 2);
        builder.Property(o => o.WaiterName).HasMaxLength(200);
        builder.Property(o => o.PaymentMethod).HasConversion<int>();
        builder.Property(o => o.PaymentStatus).HasConversion<int>();
        builder.Property(o => o.AmountTendered).HasPrecision(18, 2);
        builder.Property(o => o.ChangeGiven).HasPrecision(18, 2);
        builder.Property(o => o.RoundingAdjustment).HasPrecision(18, 2);
        builder.Property(o => o.RowVersion).IsRowVersion();
        builder.Property(o => o.StockSyncStatus).HasConversion<int>().HasDefaultValue(StockSyncStatus.NotApplicable);

        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.Subtotal);
        builder.Ignore(o => o.GrandTotal);
        builder.Ignore(o => o.AmountPaid);
        builder.Ignore(o => o.BalanceDue);

        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.RestaurantTableId);
        builder.HasIndex(o => o.DiningSessionId);
        builder.HasIndex(o => o.OrderedAtUtc);
        builder.HasIndex(o => new { o.Status, o.OrderedAtUtc });
        builder.HasIndex(o => new { o.PaymentStatus, o.OrderedAtUtc });
        builder.HasIndex(o => o.AccountedAtUtc);
        builder.HasIndex(o => o.StockSyncStatus);

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Payments)
            .WithOne()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Order.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Order.Payments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RestaurantTable>()
            .WithMany()
            .HasForeignKey(o => o.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DiningSession>()
            .WithMany()
            .HasForeignKey(o => o.DiningSessionId)
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
        builder.Property(l => l.StationName).HasMaxLength(80);
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.TaxRatePercentSnapshot).HasPrecision(5, 2);
        builder.Property(l => l.TaxableAmountSnapshot).HasPrecision(18, 2);
        builder.Property(l => l.TaxAmountSnapshot).HasPrecision(18, 2);

        builder.Ignore(l => l.LineTotal);
        builder.Ignore(l => l.ModifiersTotal);
        builder.Ignore(l => l.EffectiveUnitPrice);

        builder.HasIndex(l => l.OrderId);
        builder.HasIndex(l => l.StationId);

        builder.HasMany(l => l.Modifiers)
            .WithOne()
            .HasForeignKey(m => m.OrderLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(OrderLine.Modifiers))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Money math everywhere reads OrderLine.LineTotal (= (unit + modifier deltas) × qty). Auto-include the
        // modifiers so every tracking load of a line carries its add-on prices — settlement, VAT, balance-due,
        // and receipts all stay correct without each query remembering to .ThenInclude them.
        builder.Navigation(l => l.Modifiers).AutoInclude();
    }
}

public class OrderLineModifierConfiguration : IEntityTypeConfiguration<OrderLineModifier>
{
    public void Configure(EntityTypeBuilder<OrderLineModifier> builder)
    {
        builder.ToTable("OrderLineModifiers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.OrderLineId).IsRequired();
        builder.Property(m => m.GroupName).IsRequired().HasMaxLength(100);
        builder.Property(m => m.OptionName).IsRequired().HasMaxLength(100);
        builder.Property(m => m.PriceDelta).HasPrecision(18, 2);

        builder.HasIndex(m => m.OrderLineId);
    }
}
