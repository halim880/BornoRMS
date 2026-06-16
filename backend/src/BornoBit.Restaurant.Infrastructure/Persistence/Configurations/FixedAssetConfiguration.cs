using BornoBit.Restaurant.Domain.FixedAssets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> builder)
    {
        builder.ToTable("FixedAssets");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssetNumber).IsRequired().HasMaxLength(40);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Cost).HasPrecision(18, 2);
        builder.Property(a => a.SalvageValue).HasPrecision(18, 2);
        builder.Property(a => a.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(a => a.Method).HasConversion<int>();
        builder.Property(a => a.Status).HasConversion<int>();

        builder.Ignore(a => a.DepreciableBase);
        builder.Ignore(a => a.NetBookValue);
        builder.Ignore(a => a.RemainingDepreciable);

        builder.HasIndex(a => a.AssetNumber).IsUnique();
        builder.HasIndex(a => a.Status);

        builder.HasMany(a => a.Entries)
            .WithOne()
            .HasForeignKey(e => e.FixedAssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(FixedAsset.Entries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class DepreciationEntryConfiguration : IEntityTypeConfiguration<DepreciationEntry>
{
    public void Configure(EntityTypeBuilder<DepreciationEntry> builder)
    {
        builder.ToTable("DepreciationEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.JournalReference).HasMaxLength(40);

        builder.HasIndex(e => new { e.FixedAssetId, e.Year, e.Month }).IsUnique();
    }
}
