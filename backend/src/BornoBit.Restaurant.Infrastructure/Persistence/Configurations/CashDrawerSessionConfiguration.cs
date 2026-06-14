using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class CashDrawerSessionConfiguration : IEntityTypeConfiguration<CashDrawerSession>
{
    public void Configure(EntityTypeBuilder<CashDrawerSession> builder)
    {
        builder.ToTable("CashDrawerSessions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DrawerNumber).IsRequired().HasMaxLength(40);
        builder.Property(d => d.CashierName).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Status).HasConversion<int>();

        builder.Property(d => d.OpeningBalance).HasPrecision(18, 2);
        builder.Property(d => d.CashReceived).HasPrecision(18, 2);
        builder.Property(d => d.CashPaidOut).HasPrecision(18, 2);
        builder.Property(d => d.CountedClosingBalance).HasPrecision(18, 2);

        builder.Property(d => d.OpenNotes).HasMaxLength(1000);
        builder.Property(d => d.CloseNotes).HasMaxLength(1000);

        builder.Ignore(d => d.ExpectedClosingBalance);
        builder.Ignore(d => d.Variance);

        builder.HasIndex(d => d.DrawerNumber).IsUnique();
        builder.HasIndex(d => new { d.CashierUserId, d.Status });
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.OpenedAtUtc);

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(d => d.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
