using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class BankReconciliationConfiguration : IEntityTypeConfiguration<BankReconciliation>
{
    public void Configure(EntityTypeBuilder<BankReconciliation> builder)
    {
        builder.ToTable("BankReconciliations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.StatementBalance).HasPrecision(18, 2);
        builder.Property(r => r.ClearedBalance).HasPrecision(18, 2);
        builder.Property(r => r.Status).HasConversion<int>();

        builder.HasIndex(r => r.CashAccountId);

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(r => r.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
