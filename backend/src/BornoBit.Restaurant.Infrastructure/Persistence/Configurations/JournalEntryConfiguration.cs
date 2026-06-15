using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryNumber).IsRequired().HasMaxLength(40);
        builder.Property(e => e.EntryDate).IsRequired();
        builder.Property(e => e.VoucherType).HasConversion<int>();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.Reference).HasMaxLength(80);
        builder.Property(e => e.Narration).HasMaxLength(1000);
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(8);

        builder.Ignore(e => e.TotalDebit);
        builder.Ignore(e => e.TotalCredit);
        builder.Ignore(e => e.IsBalanced);

        builder.HasIndex(e => e.EntryNumber).IsUnique();
        builder.HasIndex(e => e.EntryDate);
        builder.HasIndex(e => e.Status);

        builder.HasMany(e => e.Lines)
            .WithOne()
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(JournalEntry.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("JournalLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.JournalEntryId).IsRequired();
        builder.Property(l => l.AccountId).IsRequired();
        builder.Property(l => l.Debit).HasPrecision(18, 2);
        builder.Property(l => l.Credit).HasPrecision(18, 2);
        builder.Property(l => l.LineNarration).HasMaxLength(500);

        builder.HasIndex(l => l.JournalEntryId);
        builder.HasIndex(l => l.AccountId);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
