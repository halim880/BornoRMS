using BornoBit.Restaurant.Domain.Dining;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("RestaurantTables");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TableNumber).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Capacity).IsRequired();

        builder.HasIndex(t => t.TableNumber).IsUnique();
    }
}
