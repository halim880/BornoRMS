using BornoBit.Restaurant.Domain.Dining;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class DiningSessionConfiguration : IEntityTypeConfiguration<DiningSession>
{
    public void Configure(EntityTypeBuilder<DiningSession> builder)
    {
        builder.ToTable("DiningSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SessionNumber).IsRequired().HasMaxLength(40);
        builder.Property(s => s.RestaurantTableId).IsRequired();
        builder.Property(s => s.WaiterName).HasMaxLength(200);
        builder.Property(s => s.Status).HasConversion<int>();
        builder.Property(s => s.CloseReason).HasMaxLength(500);

        builder.HasIndex(s => s.SessionNumber).IsUnique();
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.RestaurantTableId);
        builder.HasIndex(s => s.WaiterUserId);

        builder.HasOne<RestaurantTable>()
            .WithMany()
            .HasForeignKey(s => s.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
