using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Posting;

/// <summary>
/// Mirrors a single-entry <see cref="FinanceTransaction"/> into a balanced double-entry
/// <see cref="JournalEntry"/> so the general ledger stays in lock-step with the cash book without any
/// extra work for staff. Cash-basis: every booked money movement becomes a 2-line journal.
/// <list type="bullet">
/// <item>Income → Dr cash-account asset / Cr category income</item>
/// <item>Expense → Dr category expense / Cr cash-account asset</item>
/// </list>
/// The mirror entry's number is derived 1:1 from the source (<c>JV-&lt;txn number&gt;</c>) so no
/// pre-save sequence counting is needed (safe inside bulk imports), and <see cref="JournalEntry.Reference"/>
/// holds the source txn number for reversal on edit/delete. Unmapped accounts fall back to Suspense.
/// </summary>
public static class GeneralLedgerPoster
{
    /// <summary>Well-known fallback account code (mirrors GeneralLedgerSeeder.SuspenseCode).</summary>
    public const string SuspenseCode = "9000";

    /// <summary>Adds the mirror journal for a finance transaction (caller commits). <paramref name="entryNumberSuffix"/>
    /// keeps the number unique when re-posting after an edit (the prior mirror is voided, not replaced).</summary>
    public static async Task PostMirrorAsync(IAppDbContext db, FinanceTransaction txn, DateTime nowUtc, CancellationToken cancellationToken, string? entryNumberSuffix = null)
    {
        var cashGl = await ResolveCashAccountGlAsync(db, txn.CashAccountId, cancellationToken);
        var categoryGl = await ResolveCategoryGlAsync(db, txn.CategoryId, cancellationToken);

        var entry = JournalEntry.Create(
            $"JV-{txn.Number}{entryNumberSuffix}", txn.OccurredOn, VoucherType.Journal,
            reference: txn.Number, narration: txn.Notes ?? txn.Reference);

        if (txn.Type == TransactionType.Income)
        {
            entry.AddLine(cashGl, debit: txn.Amount, credit: 0m);
            entry.AddLine(categoryGl, debit: 0m, credit: txn.Amount);
        }
        else
        {
            entry.AddLine(categoryGl, debit: txn.Amount, credit: 0m);
            entry.AddLine(cashGl, debit: 0m, credit: txn.Amount);
        }

        entry.Post(nowUtc);
        db.JournalEntries.Add(entry);
    }

    /// <summary>Voids any live mirror journals for a transaction (on edit/delete of a manual entry).
    /// Returns the count of journals already recorded for this txn (used to build a unique re-post suffix).</summary>
    public static async Task<int> VoidMirrorsAsync(IAppDbContext db, string txnNumber, CancellationToken cancellationToken)
    {
        var entries = await db.JournalEntries
            .Where(e => e.Reference == txnNumber)
            .ToListAsync(cancellationToken);
        foreach (var e in entries.Where(e => e.Status != JournalStatus.Void)) e.Void();
        return entries.Count;
    }

    private static async Task<Guid> ResolveCashAccountGlAsync(IAppDbContext db, Guid cashAccountId, CancellationToken cancellationToken)
    {
        var ca = await db.CashAccounts.FirstOrDefaultAsync(a => a.Id == cashAccountId, cancellationToken);
        return ca is null ? await ChartOfAccountsMapper.SuspenseIdAsync(db, cancellationToken)
                          : await ChartOfAccountsMapper.EnsureCashAccountGlAsync(db, ca, cancellationToken);
    }

    private static async Task<Guid> ResolveCategoryGlAsync(IAppDbContext db, Guid categoryId, CancellationToken cancellationToken)
    {
        var cat = await db.FinanceCategories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        return cat is null ? await ChartOfAccountsMapper.SuspenseIdAsync(db, cancellationToken)
                           : await ChartOfAccountsMapper.EnsureCategoryGlAsync(db, cat, cancellationToken);
    }
}
