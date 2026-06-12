using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class FinanceCategoryConfiguration : IEntityTypeConfiguration<FinanceCategory>
{
    public void Configure(EntityTypeBuilder<FinanceCategory> builder)
    {
        builder.ToTable("FinanceCategories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(120);
        builder.Property(c => c.Type).HasConversion<int>();

        builder.HasIndex(c => c.Type);
    }
}
