using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenEntity = BornoBit.Restaurant.Domain.Kitchen.Kitchen;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class KitchenConfiguration : IEntityTypeConfiguration<KitchenEntity>
{
    public void Configure(EntityTypeBuilder<KitchenEntity> builder)
    {
        builder.ToTable("Kitchens");
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name).IsRequired().HasMaxLength(80);
        builder.Property(k => k.Code).HasMaxLength(20);
        builder.Property(k => k.ColorHex).HasMaxLength(9);
        builder.Property(k => k.PrinterName).HasMaxLength(120);

        builder.HasIndex(k => new { k.IsActive, k.DisplayOrder });
        builder.HasIndex(k => k.Name);
    }
}
