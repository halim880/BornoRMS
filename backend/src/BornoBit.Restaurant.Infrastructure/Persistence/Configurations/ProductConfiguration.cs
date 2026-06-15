using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.Kitchen;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired().HasMaxLength(40);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.BanglaName).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(8);
        builder.Property(p => p.ImagePath).HasMaxLength(500);
        builder.Property(p => p.InventoryMethod).HasConversion<int>().HasDefaultValue(InventoryMethod.None);
        builder.Property(p => p.IsCombo).HasDefaultValue(false);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.ProductCategoryId);
        builder.HasIndex(p => p.DisplayOrder);
        builder.HasIndex(p => p.KitchenStationId);

        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(p => p.ProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<KitchenStation>()
            .WithMany()
            .HasForeignKey(p => p.KitchenStationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.OptionGroups)
            .WithOne()
            .HasForeignKey(g => g.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ComboComponents)
            .WithOne()
            .HasForeignKey(c => c.ComboProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Product.Variants))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Product.OptionGroups))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Product.ComboComponents))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ProductOptionGroupConfiguration : IEntityTypeConfiguration<ProductOptionGroup>
{
    public void Configure(EntityTypeBuilder<ProductOptionGroup> builder)
    {
        builder.ToTable("ProductOptionGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.ProductId).IsRequired();
        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.BanglaName).HasMaxLength(100);

        builder.Ignore(g => g.IsRequired);
        builder.Ignore(g => g.IsSingleSelect);

        builder.HasIndex(g => new { g.ProductId, g.DisplayOrder });

        builder.HasMany(g => g.Options)
            .WithOne()
            .HasForeignKey(o => o.OptionGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ProductOptionGroup.Options))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.ToTable("ProductOptions");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OptionGroupId).IsRequired();
        builder.Property(o => o.Name).IsRequired().HasMaxLength(100);
        builder.Property(o => o.BanglaName).HasMaxLength(100);
        builder.Property(o => o.PriceDelta).HasPrecision(18, 2);

        builder.HasIndex(o => new { o.OptionGroupId, o.DisplayOrder });
    }
}

public class ComboComponentConfiguration : IEntityTypeConfiguration<ComboComponent>
{
    public void Configure(EntityTypeBuilder<ComboComponent> builder)
    {
        builder.ToTable("ComboComponents");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ComboProductId).IsRequired();
        builder.Property(c => c.ComponentProductId).IsRequired();
        builder.Property(c => c.Quantity).IsRequired();

        builder.HasIndex(c => new { c.ComboProductId, c.DisplayOrder });

        // Component points at a normal product; Restrict so deleting a product used in a combo is blocked.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(c => c.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ProductId).IsRequired();
        builder.Property(v => v.Name).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Price).HasPrecision(18, 2);

        builder.HasIndex(v => new { v.ProductId, v.DisplayOrder });
    }
}
