using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

// GL-derived financial statements. Both read POSTED journal lines only, so they reflect every auto-posted
// mirror plus any manual journals — a single double-entry source of truth.

public record GlAccountLineDto(string Code, string Name, decimal Amount);

// ---------- Profit & Loss (GL) ----------

/// <summary>Income − Expense over a period, from posted journal lines grouped by account.</summary>
public record GetGlProfitAndLossQuery(DateTime? FromDate = null, DateTime? ToDate = null) : IRequest<GlProfitAndLossDto>;

public record GlProfitAndLossDto(
    IReadOnlyList<GlAccountLineDto> Income,
    decimal TotalIncome,
    IReadOnlyList<GlAccountLineDto> Expense,
    decimal TotalExpense,
    decimal NetProfit);

public class GetGlProfitAndLossQueryHandler : IRequestHandler<GetGlProfitAndLossQuery, GlProfitAndLossDto>
{
    private readonly IAppDbContext _db;
    public GetGlProfitAndLossQueryHandler(IAppDbContext db) => _db = db;

    public async Task<GlProfitAndLossDto> Handle(GetGlProfitAndLossQuery request, CancellationToken cancellationToken)
    {
        var sums = await GlSums.PostedSumsByAccountAsync(_db, request.FromDate, request.ToDate, cancellationToken);

        // Income is credit-normal (credit − debit); expense is debit-normal (debit − credit).
        var income = sums.Where(s => s.Type == AccountType.Income)
            .Select(s => new GlAccountLineDto(s.Code, s.Name, s.Credit - s.Debit))
            .Where(l => l.Amount != 0m).OrderByDescending(l => l.Amount).ToList();
        var expense = sums.Where(s => s.Type == AccountType.Expense)
            .Select(s => new GlAccountLineDto(s.Code, s.Name, s.Debit - s.Credit))
            .Where(l => l.Amount != 0m).OrderByDescending(l => l.Amount).ToList();

        var totalIncome = income.Sum(l => l.Amount);
        var totalExpense = expense.Sum(l => l.Amount);
        return new GlProfitAndLossDto(income, totalIncome, expense, totalExpense, totalIncome - totalExpense);
    }
}

// ---------- Balance Sheet (GL) ----------

/// <summary>Assets vs Liabilities + Equity as of a date (inclusive). Current-period profit is shown within equity.</summary>
public record GetBalanceSheetQuery(DateTime? AsOf = null) : IRequest<BalanceSheetDto>;

public record BalanceSheetDto(
    IReadOnlyList<GlAccountLineDto> Assets,
    decimal TotalAssets,
    IReadOnlyList<GlAccountLineDto> Liabilities,
    decimal TotalLiabilities,
    IReadOnlyList<GlAccountLineDto> Equity,
    decimal CurrentEarnings,
    decimal TotalEquity,
    bool IsBalanced);

public class GetBalanceSheetQueryHandler : IRequestHandler<GetBalanceSheetQuery, BalanceSheetDto>
{
    private readonly IAppDbContext _db;
    public GetBalanceSheetQueryHandler(IAppDbContext db) => _db = db;

    public async Task<BalanceSheetDto> Handle(GetBalanceSheetQuery request, CancellationToken cancellationToken)
    {
        var sums = await GlSums.PostedSumsByAccountAsync(_db, null, request.AsOf, cancellationToken);

        var assets = sums.Where(s => s.Type == AccountType.Asset)
            .Select(s => new GlAccountLineDto(s.Code, s.Name, s.Debit - s.Credit))
            .Where(l => l.Amount != 0m).OrderBy(l => l.Code).ToList();
        var liabilities = sums.Where(s => s.Type == AccountType.Liability)
            .Select(s => new GlAccountLineDto(s.Code, s.Name, s.Credit - s.Debit))
            .Where(l => l.Amount != 0m).OrderBy(l => l.Code).ToList();
        var equity = sums.Where(s => s.Type == AccountType.Equity)
            .Select(s => new GlAccountLineDto(s.Code, s.Name, s.Credit - s.Debit))
            .Where(l => l.Amount != 0m).OrderBy(l => l.Code).ToList();

        // Current-period earnings (Income − Expense) not yet closed into retained earnings sit in equity.
        var currentEarnings =
            sums.Where(s => s.Type == AccountType.Income).Sum(s => s.Credit - s.Debit)
            - sums.Where(s => s.Type == AccountType.Expense).Sum(s => s.Debit - s.Credit);

        var totalAssets = assets.Sum(l => l.Amount);
        var totalLiabilities = liabilities.Sum(l => l.Amount);
        var totalEquity = equity.Sum(l => l.Amount) + currentEarnings;

        return new BalanceSheetDto(
            assets, totalAssets,
            liabilities, totalLiabilities,
            equity, currentEarnings, totalEquity,
            Math.Round(totalAssets, 2) == Math.Round(totalLiabilities + totalEquity, 2));
    }
}

// ---------- shared ----------

internal record AccountSum(string Code, string Name, AccountType Type, decimal Debit, decimal Credit);

internal static class GlSums
{
    /// <summary>Sums posted journal lines per postable account (optionally within a date range).</summary>
    internal static async Task<List<AccountSum>> PostedSumsByAccountAsync(
        IAppDbContext db, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
    {
        var lines =
            from l in db.JournalLines
            join e in db.JournalEntries on l.JournalEntryId equals e.Id
            where e.Status == JournalStatus.Posted
            select new { l.AccountId, l.Debit, l.Credit, e.EntryDate };

        if (fromDate is { } from) lines = lines.Where(x => x.EntryDate >= from);
        if (toDate is { } to) lines = lines.Where(x => x.EntryDate < to.Date.AddDays(1));

        var grouped = await lines
            .GroupBy(x => x.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(cancellationToken);

        var accounts = await db.Accounts
            .Select(a => new { a.Id, a.Code, a.Name, a.AccountType })
            .ToListAsync(cancellationToken);
        var byId = accounts.ToDictionary(a => a.Id);

        var result = new List<AccountSum>();
        foreach (var g in grouped)
            if (byId.TryGetValue(g.AccountId, out var a))
                result.Add(new AccountSum(a.Code, a.Name, a.AccountType, g.Debit, g.Credit));
        return result;
    }
}
