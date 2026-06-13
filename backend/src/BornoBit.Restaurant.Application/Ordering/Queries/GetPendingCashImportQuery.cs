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

        // Mirrors GetCashSummaryQuery, but limited to un-accounted, non-cancelled invoices.
        var pending = await _db.Orders
            .Where(o => o.IsPaid
                        && o.Status != OrderStatus.Cancelled
                        && o.AccountedAtUtc == null
                        && o.PaidAtUtc != null
                        && o.PaidAtUtc >= start
                        && o.PaidAtUtc < end
                        && o.PaymentMethod != null)
            .Select(o => new
            {
                Method = o.PaymentMethod!.Value,
                Total = Math.Max(
                    0m,
                    (o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m)
                        - o.DiscountAmount + o.RoundingAdjustment),
                o.Currency
            })
            .ToListAsync(cancellationToken);

        var byMethod = pending
            .GroupBy(p => p.Method)
            .Select(g => new CashMethodLineDto(g.Key, g.Count(), g.Sum(x => x.Total)))
            .OrderBy(l => l.Method)
            .ToList();

        var currency = pending.Select(p => p.Currency).FirstOrDefault() ?? "Tk";

        return new CashSummaryDto(date, byMethod, pending.Count, pending.Sum(p => p.Total), currency);
    }
}
