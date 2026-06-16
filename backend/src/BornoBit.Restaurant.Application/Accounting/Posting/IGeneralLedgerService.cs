using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Application.Accounting.Posting;

/// <summary>
/// One posted line of a direct journal. Identify the account by <see cref="AccountCode"/> (resolved
/// against the seeded chart) OR by a pre-resolved <see cref="AccountId"/> (e.g. a cash-account leaf
/// returned by <see cref="ChartOfAccountsMapper"/>). Exactly one of debit/credit is positive.
/// </summary>
public readonly record struct GlPostingLine(
    string? AccountCode,
    Guid? AccountId,
    decimal Debit,
    decimal Credit,
    string? Narration = null)
{
    public static GlPostingLine Dr(string accountCode, decimal amount, string? narration = null)
        => new(accountCode, null, amount, 0m, narration);

    public static GlPostingLine Cr(string accountCode, decimal amount, string? narration = null)
        => new(accountCode, null, 0m, amount, narration);

    public static GlPostingLine DrId(Guid accountId, decimal amount, string? narration = null)
        => new(null, accountId, amount, 0m, narration);

    public static GlPostingLine CrId(Guid accountId, decimal amount, string? narration = null)
        => new(null, accountId, 0m, amount, narration);
}

/// <summary>
/// Posts arbitrary balanced multi-line journal entries straight to the GL — the accrual counterpart to
/// the cash-mirror <see cref="GeneralLedgerPoster"/> (which can only express a 2-line cash↔category move).
/// Used for Accounts Payable, VAT Payable, depreciation, payroll and period-close entries that have no
/// cash side. Stateless: the caller passes its own <see cref="IAppDbContext"/> so the write shares the
/// handler's single <c>SaveChangesAsync</c>; this service never commits.
/// </summary>
public interface IGeneralLedgerService
{
    /// <summary>Builds, posts (freezes) and tracks a balanced journal entry. Throws if the period is
    /// closed, an account is missing/not-postable/inactive, or the lines do not balance.</summary>
    Task<JournalEntry> PostAsync(
        IAppDbContext db,
        DateTime entryDate,
        VoucherType voucherType,
        IReadOnlyList<GlPostingLine> lines,
        string? reference,
        string? narration,
        CancellationToken cancellationToken);

    /// <summary>Voids every live journal entry carrying <paramref name="reference"/> (reversal on
    /// edit/delete). Returns the count of entries already recorded for that reference.</summary>
    Task<int> VoidByReferenceAsync(IAppDbContext db, string reference, CancellationToken cancellationToken);
}
