using BornoBit.Restaurant.Domain.Numbering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class NumberingScopeConfiguration : IEntityTypeConfiguration<NumberingScope>
{
    public void Configure(EntityTypeBuilder<NumberingScope> builder)
    {
        builder.ToTable("NumberingScopes");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Prefix).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Cadence).HasConversion<byte>();
        builder.Property(s => s.Digits);

        builder.HasIndex(s => s.Code).IsUnique();
    }
}
