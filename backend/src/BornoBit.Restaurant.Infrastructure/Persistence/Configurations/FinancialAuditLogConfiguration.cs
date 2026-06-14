using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class FinancialAuditLogConfiguration : IEntityTypeConfiguration<FinancialAuditLog>
{
    public void Configure(EntityTypeBuilder<FinancialAuditLog> builder)
    {
        builder.ToTable("FinancialAuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Action).HasConversion<int>();
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(80);
        builder.Property(a => a.OrderNumber).HasMaxLength(40);
        builder.Property(a => a.Amount).HasPrecision(18, 2);
        builder.Property(a => a.BeforeJson).HasMaxLength(4000);
        builder.Property(a => a.AfterJson).HasMaxLength(4000);
        builder.Property(a => a.Notes).HasMaxLength(1000);

        builder.HasIndex(a => a.TimestampUtc);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.Action);
    }
}
