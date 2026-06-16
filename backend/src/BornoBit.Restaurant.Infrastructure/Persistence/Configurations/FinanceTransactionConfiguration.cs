using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class FinanceTransactionConfiguration : IEntityTypeConfiguration<FinanceTransaction>
{
    public void Configure(EntityTypeBuilder<FinanceTransaction> builder)
    {
        builder.ToTable("FinanceTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Number).IsRequired().HasMaxLength(40);
        builder.Property(t => t.Type).HasConversion<int>();
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Reference).HasMaxLength(80);
        builder.Property(t => t.Notes).HasMaxLength(1000);

        builder.HasIndex(t => t.Number).IsUnique();
        builder.HasIndex(t => t.OccurredOn);
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.CashAccountId);
        builder.HasIndex(t => t.CategoryId);
        builder.HasIndex(t => t.BankReconciliationId);

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(t => t.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinanceCategory>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
