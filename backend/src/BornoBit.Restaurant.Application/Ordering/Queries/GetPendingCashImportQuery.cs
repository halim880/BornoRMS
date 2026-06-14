using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>
/// Takings for a single day (UTC) that have <b>not yet been imported into the accounts</b> — paid,
/// non-cancelled invoices with <see cref="Order.AccountedAtUtc"/> still null, grouped by payment method.
/// Drives the Cash Counter import modal so the same invoice is never offered twice.
/// </summary>
public record GetPendingCashImportQuery(DateOnly Date) : IRequest<CashSummaryDto>;

public class GetPendingCashImportQueryHandler : IRequestHandler<GetPendingCashImportQuery, CashSummaryDto>
{
    private readonly IAppDbContext _db;

    public GetPendingCashImportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CashSummaryDto> Handle(GetPendingCashImportQuery request, CancellationToken cancellationToken)
    {
        var date = request.Date;
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);

        // Captured payments on paid, non-cancelled, un-accounted invoices for the day, grouped by method.
        var pending = await (
            from p in _db.Payments
            join o in _db.Orders on p.OrderId equals o.Id
            where p.Status == PaymentEntryStatus.Captured
                && o.IsPaid && o.Status != OrderStatus.Cancelled && o.AccountedAtUtc == null
                && o.PaidAtUtc != null && o.PaidAtUtc >= start && o.PaidAtUtc < end
            select new { p.Method, p.Kind, p.Amount })
            .ToListAsync(cancellationToken);

        var byMethod = pending
            .GroupBy(p => p.Method)
            .Select(g => new CashMethodLineDto(
                g.Key,
                g.Count(x => x.Kind == PaymentKind.Charge),
                g.Sum(x => x.Kind == PaymentKind.Charge ? x.Amount : -x.Amount)))
            .OrderBy(l => l.Method)
            .ToList();

        return new CashSummaryDto(date, byMethod, byMethod.Sum(m => m.Count), byMethod.Sum(m => m.Amount), "Tk");
    }
}
