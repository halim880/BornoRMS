using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(160);
        builder.Property(a => a.AccountType).HasConversion<int>();
        builder.Property(a => a.Description).HasMaxLength(500);

        builder.Ignore(a => a.NormalBalance);

        builder.HasIndex(a => a.Code).IsUnique();
        builder.HasIndex(a => a.ParentId);
        builder.HasIndex(a => a.AccountType);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(a => a.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
