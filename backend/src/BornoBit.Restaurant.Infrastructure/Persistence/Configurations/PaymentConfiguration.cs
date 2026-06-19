using BornoBit.Restaurant.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        // Id is assigned by the domain (BaseEntity = Guid.NewGuid()). Without this, EF's convention treats
        // the Guid key as store-generated and, when a new Payment is added to an already-tracked Order, the
        // "key is set" heuristic marks it Modified → UPDATE a non-existent row → DbUpdateConcurrencyException.
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.OrderId).IsRequired();
        builder.Property(p => p.Method).HasConversion<int>();
        builder.Property(p => p.Provider).HasConversion<int>();
        builder.Property(p => p.Kind).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Tendered).HasPrecision(18, 2);
        builder.Property(p => p.Change).HasPrecision(18, 2);

        builder.Property(p => p.CashierName).HasMaxLength(200);
        builder.Property(p => p.Reference).HasMaxLength(200);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.VoidReason).HasMaxLength(500);
        builder.Property(p => p.IdempotencyKey).HasMaxLength(100);

        builder.Ignore(p => p.SignedAmount);

        builder.HasIndex(p => p.OrderId);
        // Non-unique: all tenders of one split/partial settle request share a key, so the handler can
        // detect (and no-op) a replayed request by looking up any payment already carrying that key.
        builder.HasIndex(p => p.IdempotencyKey);
        builder.HasIndex(p => p.CreatedAtUtc);
        builder.HasIndex(p => p.Method);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CashDrawerSessionId);
        builder.HasIndex(p => new { p.CreatedAtUtc, p.Method });
    }
}
