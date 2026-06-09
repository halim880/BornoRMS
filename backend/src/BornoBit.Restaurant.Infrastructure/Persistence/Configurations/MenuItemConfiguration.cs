using BornoBit.Restaurant.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code).IsRequired().HasMaxLength(40);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Description).HasMaxLength(1000);
        builder.Property(i => i.Price).HasPrecision(18, 2);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(8);
        builder.Property(i => i.ImageUrl).HasMaxLength(500);

        builder.HasIndex(i => i.Code).IsUnique();
        builder.HasIndex(i => i.MenuCategoryId);

        builder.HasOne<MenuCategory>()
            .WithMany()
            .HasForeignKey(i => i.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
