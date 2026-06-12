using BornoBit.Restaurant.Domain.Catalog;
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

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.ProductCategoryId);
        builder.HasIndex(p => p.DisplayOrder);

        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(p => p.ProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Product.Variants))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
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
