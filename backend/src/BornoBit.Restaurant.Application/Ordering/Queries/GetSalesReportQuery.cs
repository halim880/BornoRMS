using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>Paid-sales totals over a date range (UTC), broken down per day.</summary>
public record GetSalesReportQuery(DateTime From, DateTime To) : IRequest<SalesReportDto>;

public record SalesReportRowDto(DateOnly Date, int OrderCount, decimal Subtotal, decimal Discount, decimal Total);

public record SalesReportDto(
    IReadOnlyList<SalesReportRowDto> Rows,
    int TotalOrders,
    decimal TotalSubtotal,
    decimal TotalDiscount,
    decimal GrandTotal,
    string Currency);

public class GetSalesReportQueryHandler : IRequestHandler<GetSalesReportQuery, SalesReportDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetSalesReportQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SalesReportDto> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.RangeUtc(DateOnly.FromDateTime(request.From), DateOnly.FromDateTime(request.To));

        // Order.Total is a C# computed property over the lines navigation, which EF cannot
        // translate, so recompute subtotal/total inline (mirrors GetCashSummaryQuery/GetOrdersQuery).
        var paid = await _db.Orders
            .Where(o => o.IsPaid
                        && o.Status != OrderStatus.Cancelled
                        && o.PaidAtUtc != null
                        && o.PaidAtUtc >= start
                        && o.PaidAtUtc < end)
            .Select(o => new
            {
                o.PaidAtUtc,
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.RoundingAdjustment,
                o.Currency
            })
            .ToListAsync(cancellationToken);

        var rows = paid
            .GroupBy(p => _clock.ToBusinessDate(p.PaidAtUtc!.Value))
            .Select(g => new SalesReportRowDto(
                g.Key,
                g.Count(),
                g.Sum(x => x.Subtotal),
                g.Sum(x => x.DiscountAmount),
                g.Sum(x => Math.Max(0m, x.Subtotal - x.DiscountAmount + x.RoundingAdjustment))))
            .OrderBy(r => r.Date)
            .ToList();

        var currency = paid.Select(p => p.Currency).FirstOrDefault() ?? "Tk";

        return new SalesReportDto(
            rows,
            rows.Sum(r => r.OrderCount),
            rows.Sum(r => r.Subtotal),
            rows.Sum(r => r.Discount),
            rows.Sum(r => r.Total),
            currency);
    }
}
