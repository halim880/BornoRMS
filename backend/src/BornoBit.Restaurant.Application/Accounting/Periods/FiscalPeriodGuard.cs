using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Periods;

/// <summary>
/// Rejects GL postings dated into a CLOSED fiscal month. Lenient by design: a month with no
/// <see cref="FiscalPeriod"/> row is treated as Open, so the ~20 existing cash-mirror callers keep
/// working until someone actually closes a period. Shared by <c>GeneralLedgerService</c> (accrual
/// postings) and <c>GeneralLedgerPoster</c> (cash mirror).
/// </summary>
public static class FiscalPeriodGuard
{
    public static async Task EnsureOpenAsync(IAppDbContext db, DateTime postingDate, CancellationToken cancellationToken)
    {
        var closed = await db.FiscalPeriods.AnyAsync(
            p => p.Year == postingDate.Year && p.Month == postingDate.Month && p.Status == FiscalPeriodStatus.Closed,
            cancellationToken);

        if (closed)
            throw new ConflictException(
                $"Accounting period {postingDate:yyyy-MM} is closed. Reopen it before posting entries dated in that month.");
    }
}
