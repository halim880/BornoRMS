using BornoBit.Restaurant.Domain.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).IsRequired().HasMaxLength(40);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Designation).HasMaxLength(120);
        builder.Property(e => e.BaseSalary).HasPrecision(18, 2);
        builder.Property(e => e.OvertimeRate).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("PayrollRuns");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RunNumber).IsRequired().HasMaxLength(40);
        builder.Property(r => r.Status).HasConversion<int>();

        builder.Ignore(r => r.TotalGross);
        builder.Ignore(r => r.TotalOvertime);
        builder.Ignore(r => r.TotalDeductions);
        builder.Ignore(r => r.TotalNet);

        builder.HasIndex(r => r.RunNumber).IsUnique();
        builder.HasIndex(r => new { r.Year, r.Month });

        builder.HasMany(r => r.Lines)
            .WithOne()
            .HasForeignKey(l => l.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(PayrollRun.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class PayrollRunLineConfiguration : IEntityTypeConfiguration<PayrollRunLine>
{
    public void Configure(EntityTypeBuilder<PayrollRunLine> builder)
    {
        builder.ToTable("PayrollRunLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Gross).HasPrecision(18, 2);
        builder.Property(l => l.Overtime).HasPrecision(18, 2);
        builder.Property(l => l.Deductions).HasPrecision(18, 2);
        builder.Ignore(l => l.Net);

        builder.HasIndex(l => l.PayrollRunId);
        builder.HasIndex(l => l.EmployeeId);
    }
}
