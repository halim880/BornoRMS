using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Payroll;

public enum EmployeeStatus { Active = 1, Inactive = 2 }

/// <summary>
/// A payroll employee. Separate from the login identity (<c>ApplicationUser</c> lives in Infrastructure and
/// Domain cannot reference it); <see cref="ApplicationUserId"/> optionally links the two, since not every
/// employee logs in and not every login is on payroll.
/// </summary>
public class Employee : AuditableEntity
{
    public string Code { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string? Designation { get; private set; }
    public decimal BaseSalary { get; private set; }
    public decimal OvertimeRate { get; private set; }
    public EmployeeStatus Status { get; private set; } = EmployeeStatus.Active;
    public Guid? ApplicationUserId { get; private set; }
    public DateTime JoinedOn { get; private set; }

    private Employee() { }

    public static Employee Create(string code, string fullName, string? designation, decimal baseSalary,
        decimal overtimeRate, DateTime joinedOn, Guid? applicationUserId = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Name is required.", nameof(fullName));
        if (baseSalary < 0m) throw new ArgumentOutOfRangeException(nameof(baseSalary));
        if (overtimeRate < 0m) throw new ArgumentOutOfRangeException(nameof(overtimeRate));

        return new Employee
        {
            Code = code.Trim().ToUpperInvariant(),
            FullName = fullName.Trim(),
            Designation = string.IsNullOrWhiteSpace(designation) ? null : designation.Trim(),
            BaseSalary = baseSalary,
            OvertimeRate = overtimeRate,
            JoinedOn = joinedOn.Date,
            ApplicationUserId = applicationUserId,
            Status = EmployeeStatus.Active
        };
    }

    public void UpdateDetails(string fullName, string? designation, decimal baseSalary, decimal overtimeRate)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Name is required.", nameof(fullName));
        if (baseSalary < 0m) throw new ArgumentOutOfRangeException(nameof(baseSalary));
        if (overtimeRate < 0m) throw new ArgumentOutOfRangeException(nameof(overtimeRate));

        FullName = fullName.Trim();
        Designation = string.IsNullOrWhiteSpace(designation) ? null : designation.Trim();
        BaseSalary = baseSalary;
        OvertimeRate = overtimeRate;
    }

    public void SetStatus(EmployeeStatus status) => Status = status;
}
