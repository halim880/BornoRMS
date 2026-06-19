using BornoBit.Restaurant.Domain.Logistics;
using BornoBit.Restaurant.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class RiderConfiguration : IEntityTypeConfiguration<Rider>
{
    public void Configure(EntityTypeBuilder<Rider> builder)
    {
        builder.ToTable("Riders");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Phone).IsRequired().HasMaxLength(40);
        builder.Property(r => r.Vehicle).HasMaxLength(120);
        builder.Property(r => r.IsActive).HasDefaultValue(true);

        builder.HasIndex(r => r.IsActive);
    }
}

public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("Deliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.OrderId).IsRequired();
        builder.Property(d => d.Status).HasConversion<int>();
        builder.Property(d => d.AddressLine).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.ContactPhone).HasMaxLength(40);
        builder.Property(d => d.FailureReason).HasMaxLength(500);

        // One delivery record per order.
        builder.HasIndex(d => d.OrderId).IsUnique();
        builder.HasIndex(d => d.RiderId);
        builder.HasIndex(d => d.Status);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Rider>()
            .WithMany()
            .HasForeignKey(d => d.RiderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
