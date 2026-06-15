using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>
/// Section-9 revenue breakdown over a date range (UTC), from paid orders. Channel revenue (QR vs staff)
/// is a proxy on the absence of a waiter; tax/service-charge totals read whatever was captured on the order.
/// </summary>
public record GetRevenueBreakdownQuery(DateTime From, DateTime To) : IRequest<RevenueBreakdownDto>;

public record RevenueBreakdownDto(
    decimal DineInRevenue,
    decimal TakeawayRevenue,
    decimal DeliveryRevenue,
    decimal QrOrderingRevenue,
    decimal DiscountAmount,
    decimal TaxCollected,
    decimal ServiceChargeCollected,
    decimal GrandTotal,
    string Currency);

public class GetRevenueBreakdownQueryHandler : IRequestHandler<GetRevenueBreakdownQuery, RevenueBreakdownDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetRevenueBreakdownQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<RevenueBreakdownDto> Handle(GetRevenueBreakdownQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.RangeUtc(DateOnly.FromDateTime(request.From), DateOnly.FromDateTime(request.To));

        var raw = await _db.Orders
            .Where(o => o.IsPaid && o.Status != OrderStatus.Cancelled
                        && o.PaidAtUtc != null && o.PaidAtUtc >= start && o.PaidAtUtc < end)
            .Select(o => new
            {
                o.OrderType,
                o.WaiterUserId,
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.RoundingAdjustment,
                o.Currency
            })
            .ToListAsync(cancellationToken);

        var paid = raw.Select(x => new
        {
            x.OrderType,
            x.WaiterUserId,
            x.DiscountAmount,
            x.TaxAmount,
            x.ServiceChargeAmount,
            x.Currency,
            Total = Math.Max(0m, x.Subtotal - x.DiscountAmount + x.TaxAmount + x.ServiceChargeAmount + x.RoundingAdjustment)
        }).ToList();

        return new RevenueBreakdownDto(
            paid.Where(x => x.OrderType == OrderType.DineIn).Sum(x => x.Total),
            paid.Where(x => x.OrderType is OrderType.Takeaway or OrderType.Collection).Sum(x => x.Total),
            paid.Where(x => x.OrderType == OrderType.Delivery).Sum(x => x.Total),
            paid.Where(x => x.WaiterUserId == null).Sum(x => x.Total),
            paid.Sum(x => x.DiscountAmount),
            paid.Sum(x => x.TaxAmount),
            paid.Sum(x => x.ServiceChargeAmount),
            paid.Sum(x => x.Total),
            paid.Select(x => x.Currency).FirstOrDefault() ?? "Tk");
    }
}
