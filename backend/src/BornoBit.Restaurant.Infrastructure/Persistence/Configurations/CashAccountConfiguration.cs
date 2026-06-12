using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class CashAccountConfiguration : IEntityTypeConfiguration<CashAccount>
{
    public void Configure(EntityTypeBuilder<CashAccount> builder)
    {
        builder.ToTable("CashAccounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(120);
        builder.Property(a => a.Kind).HasConversion<int>();
        builder.Property(a => a.OpeningBalance).HasPrecision(18, 2);

        builder.HasIndex(a => a.Name).IsUnique();
    }
}
