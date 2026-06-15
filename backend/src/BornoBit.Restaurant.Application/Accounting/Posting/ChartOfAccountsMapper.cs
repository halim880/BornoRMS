using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Posting;

/// <summary>
/// Single source of truth for the GL account a cash account / finance category posts to. Used by the
/// seeder (initial map), the poster (lazy map at first money movement) and the create commands
/// (immediate map so a new account/category shows in the chart at once). Each cash account lands under
/// its kind group — Cash / Bank / MFS — so the chart reads naturally for Bangladesh.
/// </summary>
public static class ChartOfAccountsMapper
{
    public const string AssetsCode = "1000";
    public const string CurrentAssetsCode = "1100";
    public const string CashGroupCode = "1110";
    public const string MfsGroupCode = "1120";
    public const string BankGroupCode = "1130";
    public const string IncomeCode = "4000";
    public const string ExpenseCode = "5000";
    public const string SuspenseCode = "9000";

    // New cash-account leaves are numbered in a dedicated band so they never collide with the fixed
    // template codes and aren't limited to the 9 slots inside a group's hundreds range.
    private const int CashLeafBandStart = 1400;
    private const int CashLeafBandEnd = 1900;

    public static string GroupCodeFor(CashAccountKind kind) => kind switch
    {
        CashAccountKind.Cash => CashGroupCode,
        CashAccountKind.Bank => BankGroupCode,
        CashAccountKind.MobileWallet => MfsGroupCode,
        _ => CashGroupCode
    };

    /// <summary>Returns the cash account's GL asset account, creating + mapping it under its kind group if needed.
    /// The entity must be tracked so the mapping persists on the caller's SaveChanges. Falls back to Suspense
    /// if the chart isn't seeded yet.</summary>
    public static async Task<Guid> EnsureCashAccountGlAsync(IAppDbContext db, CashAccount cashAccount, CancellationToken cancellationToken)
    {
        if (cashAccount.GlAccountId is { } existing) return existing;

        var groupCode = GroupCodeFor(cashAccount.Kind);
        var group = await db.Accounts.FirstOrDefaultAsync(a => a.Code == groupCode, cancellationToken);
        if (group is null) return await SuspenseIdAsync(db, cancellationToken);

        var code = (await NextCodeAsync(db, CashLeafBandStart, CashLeafBandEnd, step: 1, cancellationToken)).ToString();
        var acc = Account.Create(code, cashAccount.Name, AccountType.Asset, group.Id, isPostable: true);
        db.Accounts.Add(acc);
        cashAccount.MapToGlAccount(acc.Id);
        return acc.Id;
    }

    /// <summary>Returns the category's GL income/expense account, creating + mapping it under the matching header if needed.</summary>
    public static async Task<Guid> EnsureCategoryGlAsync(IAppDbContext db, FinanceCategory category, CancellationToken cancellationToken)
    {
        if (category.GlAccountId is { } existing) return existing;

        var isIncome = category.Type == TransactionType.Income;
        var headerCode = isIncome ? IncomeCode : ExpenseCode;
        var header = await db.Accounts.FirstOrDefaultAsync(a => a.Code == headerCode, cancellationToken);
        if (header is null) return await SuspenseIdAsync(db, cancellationToken);

        var (lo, hi) = isIncome ? (4000, 5000) : (5000, 6000);
        var code = (await NextCodeAsync(db, lo, hi, step: 10, cancellationToken)).ToString();
        var acc = Account.Create(code, category.Name, isIncome ? AccountType.Income : AccountType.Expense, header.Id, isPostable: true);
        db.Accounts.Add(acc);
        category.MapToGlAccount(acc.Id);
        return acc.Id;
    }

    public static async Task<Guid> SuspenseIdAsync(IAppDbContext db, CancellationToken cancellationToken)
        => await db.Accounts.Where(a => a.Code == SuspenseCode).Select(a => a.Id).FirstAsync(cancellationToken);

    // Next free 4-digit leaf code in [rangeStart, rangeEnd) — max existing + step, or rangeStart + step if none.
    private static async Task<int> NextCodeAsync(IAppDbContext db, int rangeStart, int rangeEnd, int step, CancellationToken cancellationToken)
    {
        var codes = await db.Accounts.Where(a => a.Code.Length == 4).Select(a => a.Code).ToListAsync(cancellationToken);
        var max = rangeStart;
        foreach (var c in codes)
            if (int.TryParse(c, out var n) && n > rangeStart && n < rangeEnd && n > max) max = n;
        return max + step;
    }
}
