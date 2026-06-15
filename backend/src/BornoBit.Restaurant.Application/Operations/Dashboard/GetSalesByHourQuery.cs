using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Section-3 line chart: paid revenue bucketed into the 24 hours of the day(s) in range (UTC).</summary>
public record GetSalesByHourQuery(DateTime From, DateTime To) : IRequest<IReadOnlyList<HourlySalesDto>>;

public record HourlySalesDto(int Hour, decimal Revenue, int OrderCount);

public class GetSalesByHourQueryHandler : IRequestHandler<GetSalesByHourQuery, IReadOnlyList<HourlySalesDto>>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetSalesByHourQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<HourlySalesDto>> Handle(GetSalesByHourQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.RangeUtc(DateOnly.FromDateTime(request.From), DateOnly.FromDateTime(request.To));

        var paid = await _db.Orders
            .Where(o => o.IsPaid && o.Status != OrderStatus.Cancelled
                        && o.PaidAtUtc != null && o.PaidAtUtc >= start && o.PaidAtUtc < end)
            .Select(o => new
            {
                Hour = o.PaidAtUtc!.Value.Hour,
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.RoundingAdjustment
            })
            .ToListAsync(cancellationToken);

        var byHour = paid
            .GroupBy(p => p.Hour)
            .ToDictionary(
                g => g.Key,
                g => (Revenue: g.Sum(x => Math.Max(0m, x.Subtotal - x.DiscountAmount + x.TaxAmount + x.ServiceChargeAmount + x.RoundingAdjustment)),
                      Count: g.Count()));

        // Dense 0..23 so the line chart always has a full axis.
        return Enumerable.Range(0, 24)
            .Select(h => byHour.TryGetValue(h, out var v)
                ? new HourlySalesDto(h, v.Revenue, v.Count)
                : new HourlySalesDto(h, 0m, 0))
            .ToList();
    }
}
