using BornoBit.Restaurant.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class RestaurantBillingSettingsConfiguration : IEntityTypeConfiguration<RestaurantBillingSettings>
{
    public void Configure(EntityTypeBuilder<RestaurantBillingSettings> builder)
    {
        builder.ToTable("RestaurantBillingSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.VatPercent).HasPrecision(5, 2);
        builder.Property(s => s.ServiceChargePercent).HasPrecision(5, 2);
        builder.Property(s => s.HighDiscountThresholdPercent).HasPrecision(5, 2);
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(8);
    }
}
