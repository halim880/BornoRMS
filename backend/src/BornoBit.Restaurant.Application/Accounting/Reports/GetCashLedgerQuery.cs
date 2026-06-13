using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

/// <summary>
/// A cash ledger over a date range. With <paramref name="CashAccountId"/> set it is a single account's
/// ledger; left null it is the combined cash book across every cash account. The opening balance carries
/// forward everything before <paramref name="From"/>; the rows accumulate a running balance; the closing
/// balance is opening + receipts − payments. Drives both the Cash Book and Account Ledger pages.
/// </summary>
public record GetCashLedgerQuery(DateTime From, DateTime To, Guid? CashAccountId) : IRequest<CashLedgerDto>;

public record CashLedgerRowDto(
    DateTime OccurredOn,
    string Number,
    string CategoryName,
    string CashAccountName,
    TransactionType Type,
    decimal In,
    decimal Out,
    decimal RunningBalance);

public record CashLedgerDto(
    decimal OpeningBalance,
    decimal TotalReceipts,
    decimal TotalPayments,
    decimal ClosingBalance,
    IReadOnlyList<CashLedgerRowDto> Rows);

public class GetCashLedgerQueryHandler : IRequestHandler<GetCashLedgerQuery, CashLedgerDto>
{
    private readonly IAppDbContext _db;

    public GetCashLedgerQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CashLedgerDto> Handle(GetCashLedgerQuery request, CancellationToken cancellationToken)
    {
        var fromDate = request.From.Date;
        var toDate = request.To.Date;

        // Scope: one account, or all cash accounts combined.
        var scoped = _db.FinanceTransactions.AsQueryable();
        if (request.CashAccountId is { } accountId)
            scoped = scoped.Where(t => t.CashAccountId == accountId);

        // Opening: account opening balance(s) + the net of everything that happened before the range.
        var accountsOpening = request.CashAccountId is { } id
            ? await _db.CashAccounts.Where(a => a.Id == id).Select(a => (decimal?)a.OpeningBalance).SumAsync(cancellationToken) ?? 0m
            : await _db.CashAccounts.Select(a => (decimal?)a.OpeningBalance).SumAsync(cancellationToken) ?? 0m;

        var priorIncome = await scoped
            .Where(t => t.OccurredOn < fromDate && t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
        var priorExpense = await scoped
            .Where(t => t.OccurredOn < fromDate && t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var openingBalance = accountsOpening + priorIncome - priorExpense;

        // In-range rows, ordered for a meaningful running balance.
        var inRange = await (
            from t in scoped
            where t.OccurredOn >= fromDate && t.OccurredOn <= toDate
            join c in _db.FinanceCategories on t.CategoryId equals c.Id
            join a in _db.CashAccounts on t.CashAccountId equals a.Id
            orderby t.OccurredOn, t.Number
            select new { t.OccurredOn, t.Number, CategoryName = c.Name, CashAccountName = a.Name, t.Type, t.Amount })
            .ToListAsync(cancellationToken);

        var running = openingBalance;
        var rows = new List<CashLedgerRowDto>(inRange.Count);
        foreach (var r in inRange)
        {
            var isIncome = r.Type == TransactionType.Income;
            running += isIncome ? r.Amount : -r.Amount;
            rows.Add(new CashLedgerRowDto(
                r.OccurredOn, r.Number, r.CategoryName, r.CashAccountName, r.Type,
                isIncome ? r.Amount : 0m,
                isIncome ? 0m : r.Amount,
                running));
        }

        var totalReceipts = rows.Sum(r => r.In);
        var totalPayments = rows.Sum(r => r.Out);

        return new CashLedgerDto(
            openingBalance,
            totalReceipts,
            totalPayments,
            openingBalance + totalReceipts - totalPayments,
            rows);
    }
}
