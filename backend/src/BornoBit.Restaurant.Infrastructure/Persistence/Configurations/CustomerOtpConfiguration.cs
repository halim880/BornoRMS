using BornoBit.Restaurant.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class CustomerOtpConfiguration : IEntityTypeConfiguration<CustomerOtp>
{
    public void Configure(EntityTypeBuilder<CustomerOtp> builder)
    {
        builder.ToTable("CustomerOtps");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.CodeHash).IsRequired().HasMaxLength(256);
        builder.Property(o => o.ExpiresAtUtc).IsRequired();
        builder.Property(o => o.AttemptsRemaining).IsRequired();

        builder.HasIndex(o => new { o.CustomerId, o.CreatedAtUtc });
    }
}
