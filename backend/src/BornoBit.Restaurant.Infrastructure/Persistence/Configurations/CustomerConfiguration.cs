using BornoBit.Restaurant.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Phone).IsRequired().HasMaxLength(40);
        builder.Property(c => c.FullName).HasMaxLength(200);
        builder.Property(c => c.Address).HasMaxLength(500);

        builder.HasIndex(c => c.Phone).IsUnique();
    }
}
