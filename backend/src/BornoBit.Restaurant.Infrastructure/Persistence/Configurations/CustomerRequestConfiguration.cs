using BornoBit.Restaurant.Domain.Dining;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class CustomerRequestConfiguration : IEntityTypeConfiguration<CustomerRequest>
{
    public void Configure(EntityTypeBuilder<CustomerRequest> builder)
    {
        builder.ToTable("CustomerRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RestaurantTableId).IsRequired();
        builder.Property(r => r.Type).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.ResolvedBy).HasMaxLength(256);
        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RestaurantTableId);
        builder.HasIndex(r => r.RequestedAtUtc);

        builder.HasOne<RestaurantTable>()
            .WithMany()
            .HasForeignKey(r => r.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TableReservationConfiguration : IEntityTypeConfiguration<TableReservation>
{
    public void Configure(EntityTypeBuilder<TableReservation> builder)
    {
        builder.ToTable("TableReservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RestaurantTableId).IsRequired();
        builder.Property(r => r.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Phone).HasMaxLength(40);
        builder.Property(r => r.PartySize).IsRequired();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RestaurantTableId);
        builder.HasIndex(r => r.ReservedForUtc);

        builder.HasOne<RestaurantTable>()
            .WithMany()
            .HasForeignKey(r => r.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
