using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

/// <summary>
/// Net balance of one GL account (by code) from POSTED journal lines, signed to its normal balance
/// (debit-normal: Dr−Cr; credit-normal: Cr−Dr). Used by accrual screens to show the live liability/asset
/// balance (e.g. VAT Payable, Accounts Payable) without rebuilding a full trial balance.
/// </summary>
public record GetGlAccountBalanceQuery(string AccountCode) : IRequest<GlAccountBalanceDto>;

public record GlAccountBalanceDto(string Code, string Name, decimal Debit, decimal Credit, decimal Balance);

public class GetGlAccountBalanceQueryHandler : IRequestHandler<GetGlAccountBalanceQuery, GlAccountBalanceDto>
{
    private readonly IAppDbContext _db;
    public GetGlAccountBalanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<GlAccountBalanceDto> Handle(GetGlAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Code == request.AccountCode, cancellationToken)
            ?? throw new NotFoundException($"GL account '{request.AccountCode}' not found.");

        var sums = await (
            from l in _db.JournalLines
            join e in _db.JournalEntries on l.JournalEntryId equals e.Id
            where l.AccountId == account.Id && e.Status == JournalStatus.Posted
            group l by 1 into g
            select new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(cancellationToken);

        var debit = sums?.Debit ?? 0m;
        var credit = sums?.Credit ?? 0m;
        var balance = account.NormalBalance == NormalBalance.Debit ? debit - credit : credit - debit;

        return new GlAccountBalanceDto(account.Code, account.Name, debit, credit, balance);
    }
}
