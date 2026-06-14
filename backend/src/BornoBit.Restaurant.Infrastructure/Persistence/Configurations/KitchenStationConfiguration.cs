using BornoBit.Restaurant.Domain.Kitchen;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
    }
}
