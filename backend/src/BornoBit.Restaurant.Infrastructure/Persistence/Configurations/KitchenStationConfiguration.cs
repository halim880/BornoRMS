using BornoBit.Restaurant.Domain.Kitchen;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenEntity = BornoBit.Restaurant.Domain.Kitchen.Kitchen;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class KitchenStationConfiguration : IEntityTypeConfiguration<KitchenStation>
{
    public void Configure(EntityTypeBuilder<KitchenStation> builder)
    {
        builder.ToTable("KitchenStations");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(80);
        builder.Property(s => s.Code).HasMaxLength(20);
        builder.Property(s => s.ColorHex).HasMaxLength(9);

        builder.HasIndex(s => new { s.IsActive, s.DisplayOrder });
        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.KitchenId);

        builder.HasOne<KitchenEntity>()
            .WithMany()
            .HasForeignKey(s => s.KitchenId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
