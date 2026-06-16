using BornoBit.Restaurant.Application.Accounting.Periods;
using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Posting;

/// <inheritdoc cref="IGeneralLedgerService" />
public class GeneralLedgerService : IGeneralLedgerService
{
    private readonly IJournalNumberGenerator _numbers;
    private readonly TimeProvider _timeProvider;

    public GeneralLedgerService(IJournalNumberGenerator numbers, TimeProvider timeProvider)
    {
        _numbers = numbers;
        _timeProvider = timeProvider;
    }

    public async Task<JournalEntry> PostAsync(
        IAppDbContext db,
        DateTime entryDate,
        VoucherType voucherType,
        IReadOnlyList<GlPostingLine> lines,
        string? reference,
        string? narration,
        CancellationToken cancellationToken)
    {
        if (lines is null || lines.Count < 2)
            throw new ConflictException("A journal entry needs at least two lines.");

        await FiscalPeriodGuard.EnsureOpenAsync(db, entryDate, cancellationToken);

        // Resolve every line to an account id, validating postable + active (mirrors CreateJournalEntryCommand).
        var byId = await ResolveAccountsAsync(db, lines, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var entryNumber = await _numbers.NextAsync(nowUtc, cancellationToken);

        var entry = JournalEntry.Create(entryNumber, entryDate, voucherType, reference, narration);
        foreach (var line in lines)
        {
            var accountId = line.AccountId ?? byId[line.AccountCode!];
            entry.AddLine(accountId, line.Debit, line.Credit, line.Narration);
        }

        entry.Post(nowUtc); // enforces balance / ≥2 lines / positive total
        db.JournalEntries.Add(entry);
        return entry;
    }

    public async Task<int> VoidByReferenceAsync(IAppDbContext db, string reference, CancellationToken cancellationToken)
    {
        var entries = await db.JournalEntries
            .Where(e => e.Reference == reference)
            .ToListAsync(cancellationToken);
        foreach (var e in entries.Where(e => e.Status != JournalStatus.Void)) e.Void();
        return entries.Count;
    }

    private static async Task<Dictionary<string, Guid>> ResolveAccountsAsync(
        IAppDbContext db, IReadOnlyList<GlPostingLine> lines, CancellationToken cancellationToken)
    {
        var codes = lines.Where(l => l.AccountCode is not null).Select(l => l.AccountCode!).Distinct().ToList();
        var ids = lines.Where(l => l.AccountId is { } id && id != Guid.Empty).Select(l => l.AccountId!.Value).Distinct().ToList();

        var accounts = await db.Accounts
            .Where(a => codes.Contains(a.Code) || ids.Contains(a.Id))
            .Select(a => new { a.Id, a.Code, a.IsPostable, a.IsActive })
            .ToListAsync(cancellationToken);

        foreach (var a in accounts)
        {
            if (!a.IsPostable) throw new ConflictException($"Account {a.Code} is not postable (group/header account).");
            if (!a.IsActive) throw new ConflictException($"Account {a.Code} is inactive and cannot be posted to.");
        }

        var byCode = accounts.ToDictionary(a => a.Code, a => a.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var code in codes)
            if (!byCode.ContainsKey(code))
                throw new NotFoundException($"GL account with code '{code}' not found. Is the chart seeded?");

        var byId = accounts.Select(a => a.Id).ToHashSet();
        foreach (var id in ids)
            if (!byId.Contains(id))
                throw new NotFoundException($"GL account {id} not found.");

        return byCode;
    }
}
