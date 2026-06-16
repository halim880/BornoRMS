using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Payroll;

public enum PayrollRunStatus { Draft = 1, Approved = 2, Paid = 3 }

/// <summary>
/// A monthly payroll run (aggregate root). Lines are editable while Draft. Approve posts the accrual
/// (Dr Salary/Overtime, Cr Employee Payable + Tax Payable); Pay settles Employee Payable against cash.
/// </summary>
public class PayrollRun : AuditableEntity
{
    public string RunNumber { get; private set; } = default!;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public PayrollRunStatus Status { get; private set; } = PayrollRunStatus.Draft;
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    private readonly List<PayrollRunLine> _lines = new();
    public IReadOnlyCollection<PayrollRunLine> Lines => _lines.AsReadOnly();

    public decimal TotalGross => _lines.Sum(l => l.Gross);
    public decimal TotalOvertime => _lines.Sum(l => l.Overtime);
    public decimal TotalDeductions => _lines.Sum(l => l.Deductions);
    public decimal TotalNet => _lines.Sum(l => l.Net);

    private PayrollRun() { }

    public static PayrollRun Create(string runNumber, int year, int month)
    {
        if (string.IsNullOrWhiteSpace(runNumber)) throw new ArgumentException("Run number is required.", nameof(runNumber));
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));

        return new PayrollRun
        {
            RunNumber = runNumber.Trim().ToUpperInvariant(),
            Year = year,
            Month = month,
            Status = PayrollRunStatus.Draft
        };
    }

    public PayrollRunLine AddLine(Guid employeeId, decimal gross, decimal overtime, decimal deductions)
    {
        if (Status != PayrollRunStatus.Draft) throw new InvalidOperationException("Only a draft run can be edited.");
        if (employeeId == Guid.Empty) throw new ArgumentException("Employee is required.", nameof(employeeId));
        if (gross < 0m || overtime < 0m || deductions < 0m) throw new ArgumentOutOfRangeException(nameof(gross));

        var line = new PayrollRunLine
        {
            PayrollRunId = Id,
            EmployeeId = employeeId,
            Gross = gross,
            Overtime = overtime,
            Deductions = deductions
        };
        _lines.Add(line);
        return line;
    }

    public void UpdateLine(Guid employeeId, decimal gross, decimal overtime, decimal deductions)
    {
        if (Status != PayrollRunStatus.Draft) throw new InvalidOperationException("Only a draft run can be edited.");
        if (gross < 0m || overtime < 0m || deductions < 0m) throw new ArgumentOutOfRangeException(nameof(gross));
        var line = _lines.FirstOrDefault(l => l.EmployeeId == employeeId)
            ?? throw new InvalidOperationException("Employee is not on this run.");
        line.Gross = gross;
        line.Overtime = overtime;
        line.Deductions = deductions;
    }

    public void Approve(DateTime nowUtc)
    {
        if (Status != PayrollRunStatus.Draft) throw new InvalidOperationException("Only a draft run can be approved.");
        if (_lines.Count == 0) throw new InvalidOperationException("A payroll run needs at least one line.");
        Status = PayrollRunStatus.Approved;
        ApprovedAtUtc = nowUtc;
    }

    public void MarkPaid(DateTime nowUtc)
    {
        if (Status != PayrollRunStatus.Approved) throw new InvalidOperationException("Only an approved run can be paid.");
        Status = PayrollRunStatus.Paid;
        PaidAtUtc = nowUtc;
    }
}

/// <summary>One employee's pay on a run. Net = Gross + Overtime − Deductions.</summary>
public class PayrollRunLine : BaseEntity
{
    public Guid PayrollRunId { get; internal set; }
    public Guid EmployeeId { get; internal set; }
    public decimal Gross { get; internal set; }
    public decimal Overtime { get; internal set; }
    public decimal Deductions { get; internal set; }
    public decimal Net => Gross + Overtime - Deductions;
}
