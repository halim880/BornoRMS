using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

/// <summary>
/// Ledger for one account over <c>Posted</c> entries, with an opening balance (everything
/// before <see cref="FromDate"/>) and a running balance. The running balance is signed on
/// the account's normal side — positive means the account sits on its natural balance side.
/// </summary>
public record GetAccountLedgerQuery(Guid AccountId, DateTime? FromDate = null, DateTime? ToDate = null)
    : IRequest<AccountLedgerDto>;

public record LedgerRowDto(
    Guid JournalEntryId,
    string EntryNumber,
    DateTime EntryDate,
    string? Narration,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance);

public record AccountLedgerDto(
    Guid AccountId,
    string Code,
    string Name,
    AccountType AccountType,
    NormalBalance NormalBalance,
    decimal OpeningBalance,
    decimal ClosingBalance,
    IReadOnlyList<LedgerRowDto> Rows);

public class GetAccountLedgerQueryHandler : IRequestHandler<GetAccountLedgerQuery, AccountLedgerDto>
{
    private readonly IAppDbContext _db;

    public GetAccountLedgerQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AccountLedgerDto> Handle(GetAccountLedgerQuery request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts.AsNoTracking()
            .Where(a => a.Id == request.AccountId)
            .Select(a => new { a.Id, a.Code, a.Name, a.AccountType })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        var normal = account.AccountType is AccountType.Asset or AccountType.Expense
            ? NormalBalance.Debit
            : NormalBalance.Credit;
        var sign = normal == NormalBalance.Debit ? 1m : -1m;

        var posted =
            from l in _db.JournalLines.AsNoTracking()
            join e in _db.JournalEntries.AsNoTracking() on l.JournalEntryId equals e.Id
            where l.AccountId == request.AccountId && e.Status == JournalStatus.Posted
            select new { l.Debit, l.Credit, e.EntryDate, e.EntryNumber, e.Id, l.LineNarration };

        // Opening: signed net of everything strictly before the window start.
        decimal opening = 0m;
        if (request.FromDate is { } from)
        {
            var before = await posted.Where(x => x.EntryDate < from)
                .Select(x => new { x.Debit, x.Credit })
                .ToListAsync(cancellationToken);
            opening = sign * before.Sum(x => x.Debit - x.Credit);
            posted = posted.Where(x => x.EntryDate >= from);
        }
        if (request.ToDate is { } to)
            posted = posted.Where(x => x.EntryDate < to.Date.AddDays(1));

        var window = await posted
            .OrderBy(x => x.EntryDate).ThenBy(x => x.EntryNumber)
            .ToListAsync(cancellationToken);

        var rows = new List<LedgerRowDto>(window.Count);
        var running = opening;
        foreach (var x in window)
        {
            running += sign * (x.Debit - x.Credit);
            rows.Add(new LedgerRowDto(x.Id, x.EntryNumber, x.EntryDate, x.LineNarration, x.Debit, x.Credit, running));
        }

        return new AccountLedgerDto(
            account.Id, account.Code, account.Name, account.AccountType, normal,
            opening, running, rows);
    }
}
