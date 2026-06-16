using BornoBit.Restaurant.Application.Accounting.Posting;
using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Payroll;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Payroll;

public record PayrollRunLineDto(Guid EmployeeId, string EmployeeName, decimal Gross, decimal Overtime, decimal Deductions, decimal Net);

public record PayrollRunDto(
    Guid Id, string RunNumber, int Year, int Month, PayrollRunStatus Status,
    decimal TotalGross, decimal TotalOvertime, decimal TotalDeductions, decimal TotalNet,
    IReadOnlyList<PayrollRunLineDto> Lines);

public record PayrollRunSummaryDto(Guid Id, string RunNumber, int Year, int Month, PayrollRunStatus Status, decimal TotalNet);

/// <summary>Create a draft payroll run for a month, seeding a line per active employee from their base salary.</summary>
public record CreatePayrollRunCommand(int Year, int Month) : IRequest<Guid>;

public class CreatePayrollRunCommandValidator : AbstractValidator<CreatePayrollRunCommand>
{
    public CreatePayrollRunCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 9999);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class CreatePayrollRunCommandHandler : IRequestHandler<CreatePayrollRunCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IPayrollNumberGenerator _numbers;
    private readonly TimeProvider _time;

    public CreatePayrollRunCommandHandler(IAppDbContext db, IPayrollNumberGenerator numbers, TimeProvider time)
    {
        _db = db;
        _numbers = numbers;
        _time = time;
    }

    public async Task<Guid> Handle(CreatePayrollRunCommand request, CancellationToken cancellationToken)
    {
        var employees = await _db.Employees.Where(e => e.Status == EmployeeStatus.Active).ToListAsync(cancellationToken);
        if (employees.Count == 0) throw new ConflictException("There are no active employees to pay.");

        var number = await _numbers.NextAsync(_time.GetUtcNow().UtcDateTime, cancellationToken);
        var run = PayrollRun.Create(number, request.Year, request.Month);
        foreach (var e in employees)
            run.AddLine(e.Id, e.BaseSalary, overtime: 0m, deductions: 0m);

        _db.PayrollRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
        return run.Id;
    }
}

/// <summary>Update one employee's line on a draft run.</summary>
public record UpdatePayrollLineCommand(Guid RunId, Guid EmployeeId, decimal Gross, decimal Overtime, decimal Deductions) : IRequest<Unit>;

public class UpdatePayrollLineCommandHandler : IRequestHandler<UpdatePayrollLineCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdatePayrollLineCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdatePayrollLineCommand request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == request.RunId, cancellationToken)
            ?? throw new NotFoundException("Payroll run not found.");
        try { run.UpdateLine(request.EmployeeId, request.Gross, request.Overtime, request.Deductions); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

/// <summary>Approve a draft run — posts the accrual: Dr Salary/Overtime, Cr Employee Payable + Tax Payable.</summary>
public record ApprovePayrollRunCommand(Guid RunId) : IRequest<Unit>;

public class ApprovePayrollRunCommandHandler : IRequestHandler<ApprovePayrollRunCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IGeneralLedgerService _gl;
    private readonly TimeProvider _time;

    public ApprovePayrollRunCommandHandler(IAppDbContext db, IGeneralLedgerService gl, TimeProvider time)
    {
        _db = db;
        _gl = gl;
        _time = time;
    }

    public async Task<Unit> Handle(ApprovePayrollRunCommand request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == request.RunId, cancellationToken)
            ?? throw new NotFoundException("Payroll run not found.");

        var nowUtc = _time.GetUtcNow().UtcDateTime;
        try { run.Approve(nowUtc); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }

        var periodDate = new DateTime(run.Year, run.Month, DateTime.DaysInMonth(run.Year, run.Month));
        var lines = new List<GlPostingLine>
        {
            GlPostingLine.Dr(GlCodes.SalaryExpense, run.TotalGross, "Gross salary"),
        };
        if (run.TotalOvertime > 0m) lines.Add(GlPostingLine.Dr(GlCodes.OvertimeExpense, run.TotalOvertime, "Overtime"));
        lines.Add(GlPostingLine.Cr(GlCodes.EmployeePayable, run.TotalNet, "Net pay payable"));
        if (run.TotalDeductions > 0m) lines.Add(GlPostingLine.Cr(GlCodes.TaxPayable, run.TotalDeductions, "Payroll deductions"));

        await _gl.PostAsync(_db, periodDate, VoucherType.Journal, lines, $"PR-{run.RunNumber}",
            $"Payroll accrual {run.RunNumber}", cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

/// <summary>Pay an approved run from a cash account — settles Employee Payable against cash (GL) and books a
/// cash-book Salary expense (cash-basis), mirroring the supplier-payment hybrid pattern.</summary>
public record PayPayrollRunCommand(Guid RunId, Guid CashAccountId) : IRequest<Unit>;

public class PayPayrollRunCommandHandler : IRequestHandler<PayPayrollRunCommand, Unit>
{
    private const string SalaryCategoryName = "Salary";

    private readonly IAppDbContext _db;
    private readonly IGeneralLedgerService _gl;
    private readonly ITransactionNumberGenerator _numbers;
    private readonly TimeProvider _time;

    public PayPayrollRunCommandHandler(IAppDbContext db, IGeneralLedgerService gl, ITransactionNumberGenerator numbers, TimeProvider time)
    {
        _db = db;
        _gl = gl;
        _numbers = numbers;
        _time = time;
    }

    public async Task<Unit> Handle(PayPayrollRunCommand request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == request.RunId, cancellationToken)
            ?? throw new NotFoundException("Payroll run not found.");

        var cashAccount = await _db.CashAccounts.FirstOrDefaultAsync(a => a.Id == request.CashAccountId && a.IsActive, cancellationToken)
            ?? throw new ValidationException("The selected cash account does not exist or is inactive.");

        var nowUtc = _time.GetUtcNow().UtcDateTime;
        try { run.MarkPaid(nowUtc); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }

        var paidOn = nowUtc.Date;

        // Cash-book Salary expense (cash-basis P&L) — not GL-mirrored.
        var salaryCategory = await _db.FinanceCategories
            .FirstOrDefaultAsync(c => c.Type == TransactionType.Expense && c.Name == SalaryCategoryName, cancellationToken);
        if (salaryCategory is null)
        {
            salaryCategory = FinanceCategory.Create(SalaryCategoryName, TransactionType.Expense);
            _db.FinanceCategories.Add(salaryCategory);
        }
        var number = await _numbers.NextAsync(nowUtc, cancellationToken);
        var txn = FinanceTransaction.Create(number, paidOn, TransactionType.Expense, cashAccount.Id, salaryCategory.Id,
            run.TotalNet, run.RunNumber, $"Payroll {run.RunNumber}");
        _db.FinanceTransactions.Add(txn);

        // GL: settle the payable against cash (Dr Employee Payable / Cr cash-leaf).
        var cashGl = await ChartOfAccountsMapper.EnsureCashAccountGlAsync(_db, cashAccount, cancellationToken);
        await _gl.PostAsync(_db, paidOn, VoucherType.Payment, new[]
        {
            GlPostingLine.Dr(GlCodes.EmployeePayable, run.TotalNet, $"Pay {run.RunNumber}"),
            GlPostingLine.CrId(cashGl, run.TotalNet, $"Paid from {cashAccount.Name}")
        }, reference: $"PRPAY-{run.RunNumber}", narration: $"Payroll payout {run.RunNumber}", cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record GetPayrollRunsQuery : IRequest<IReadOnlyList<PayrollRunSummaryDto>>;

public class GetPayrollRunsQueryHandler : IRequestHandler<GetPayrollRunsQuery, IReadOnlyList<PayrollRunSummaryDto>>
{
    private readonly IAppDbContext _db;
    public GetPayrollRunsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayrollRunSummaryDto>> Handle(GetPayrollRunsQuery request, CancellationToken cancellationToken)
    {
        var runs = await _db.PayrollRuns
            .Include(r => r.Lines)
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ThenByDescending(r => r.RunNumber)
            .ToListAsync(cancellationToken);

        return runs
            .Select(r => new PayrollRunSummaryDto(r.Id, r.RunNumber, r.Year, r.Month, r.Status, r.TotalNet))
            .ToList();
    }
}

public record GetPayrollRunQuery(Guid Id) : IRequest<PayrollRunDto>;

public class GetPayrollRunQueryHandler : IRequestHandler<GetPayrollRunQuery, PayrollRunDto>
{
    private readonly IAppDbContext _db;
    public GetPayrollRunQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PayrollRunDto> Handle(GetPayrollRunQuery request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Payroll run not found.");

        var employeeNames = await _db.Employees
            .Where(e => run.Lines.Select(l => l.EmployeeId).Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FullName, cancellationToken);

        var lines = run.Lines
            .Select(l => new PayrollRunLineDto(
                l.EmployeeId, employeeNames.GetValueOrDefault(l.EmployeeId, "—"),
                l.Gross, l.Overtime, l.Deductions, l.Net))
            .OrderBy(l => l.EmployeeName)
            .ToList();

        return new PayrollRunDto(run.Id, run.RunNumber, run.Year, run.Month, run.Status,
            run.TotalGross, run.TotalOvertime, run.TotalDeductions, run.TotalNet, lines);
    }
}
