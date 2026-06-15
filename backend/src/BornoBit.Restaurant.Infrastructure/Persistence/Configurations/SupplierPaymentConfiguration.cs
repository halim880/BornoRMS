using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.ToTable("SupplierPayments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Reference).HasMaxLength(80);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.HasIndex(p => p.SupplierId);
        builder.HasIndex(p => p.PaidOn);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(p => p.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
