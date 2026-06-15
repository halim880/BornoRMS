using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

/// <summary>
/// Trial balance over <c>Posted</c> journal lines. Each account's net balance is shown on
/// its natural side (debit or credit); a balanced ledger has equal column totals.
/// </summary>
public record GetTrialBalanceQuery(DateTime? FromDate = null, DateTime? ToDate = null)
    : IRequest<TrialBalanceDto>;

public record TrialBalanceRowDto(
    Guid AccountId,
    string Code,
    string Name,
    AccountType AccountType,
    decimal Debit,
    decimal Credit);

public record TrialBalanceDto(
    DateTime? FromDate,
    DateTime? ToDate,
    IReadOnlyList<TrialBalanceRowDto> Rows,
    decimal TotalDebit,
    decimal TotalCredit)
{
    public bool IsBalanced => TotalDebit == TotalCredit;
}

public class GetTrialBalanceQueryHandler : IRequestHandler<GetTrialBalanceQuery, TrialBalanceDto>
{
    private readonly IAppDbContext _db;

    public GetTrialBalanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<TrialBalanceDto> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken)
    {
        var lines =
            from l in _db.JournalLines.AsNoTracking()
            join e in _db.JournalEntries.AsNoTracking() on l.JournalEntryId equals e.Id
            where e.Status == JournalStatus.Posted
            select new { l.AccountId, l.Debit, l.Credit, e.EntryDate };

        if (request.FromDate is { } from) lines = lines.Where(x => x.EntryDate >= from);
        if (request.ToDate is { } to) lines = lines.Where(x => x.EntryDate < to.Date.AddDays(1));

        var sums = await lines
            .GroupBy(x => x.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(cancellationToken);

        var accounts = await _db.Accounts.AsNoTracking()
            .Select(a => new { a.Id, a.Code, a.Name, a.AccountType })
            .ToListAsync(cancellationToken);
        var byId = accounts.ToDictionary(a => a.Id);

        var rows = new List<TrialBalanceRowDto>();
        foreach (var s in sums)
        {
            if (!byId.TryGetValue(s.AccountId, out var acc)) continue;
            var net = s.Debit - s.Credit; // signed on the debit side
            var debit = net > 0m ? net : 0m;
            var credit = net < 0m ? -net : 0m;
            if (debit == 0m && credit == 0m) continue; // net-zero accounts add nothing
            rows.Add(new TrialBalanceRowDto(acc.Id, acc.Code, acc.Name, acc.AccountType, debit, credit));
        }

        rows = rows.OrderBy(r => r.Code).ToList();

        return new TrialBalanceDto(
            request.FromDate,
            request.ToDate,
            rows,
            rows.Sum(r => r.Debit),
            rows.Sum(r => r.Credit));
    }
}
