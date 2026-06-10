using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Code).IsRequired().HasMaxLength(16);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(80);
        builder.Property(u => u.BanglaName).HasMaxLength(80);
        builder.Property(u => u.Dimension).HasConversion<int>();
        builder.Property(u => u.ToBaseFactor).HasPrecision(18, 6);

        builder.HasIndex(u => u.Code).IsUnique();
    }
}
