using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>
/// KPI tiles + per-status tab counts for the back-office Orders screen. Applies the
/// same scoping filters as <see cref="GetOrdersQuery"/> (date window / search / order
/// number) but deliberately ignores the status filter, so the counts and tiles
/// describe the whole window across every status.
/// </summary>
public record GetOrdersSummaryQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    string? Search = null,
    string? OrderNumber = null
) : IRequest<OrdersSummaryDto>;

public record OrdersSummaryDto(
    int TotalOrders,
    int ActiveOrders,
    decimal PaidRevenue,
    decimal Outstanding,
    string Currency,
    Dictionary<string, int> StatusCounts);

public class GetOrdersSummaryQueryHandler : IRequestHandler<GetOrdersSummaryQuery, OrdersSummaryDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetOrdersSummaryQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<OrdersSummaryDto> Handle(GetOrdersSummaryQuery request, CancellationToken cancellationToken)
    {
        var query =
            from o in _db.Orders
            join c in _db.Customers on o.CustomerId equals c.Id
            join t in _db.RestaurantTables on o.RestaurantTableId equals t.Id into tj
            from t in tj.DefaultIfEmpty()
            select new { Order = o, Customer = c, Table = t };

        if (request.From is { } from)
        {
            var (start, _) = _clock.DayWindowUtc(from);
            query = query.Where(x => x.Order.OrderedAtUtc >= start);
        }

        if (request.To is { } to)
        {
            var (_, end) = _clock.DayWindowUtc(to);
            query = query.Where(x => x.Order.OrderedAtUtc < end);
        }

        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
        {
            var on = request.OrderNumber.Trim().ToUpper();
            query = query.Where(x => x.Order.OrderNumber.Contains(on));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.Order.OrderNumber.Contains(term)
                || (x.Customer.FullName != null && x.Customer.FullName.Contains(term))
                || x.Customer.Phone.Contains(term)
                || (x.Table != null && x.Table.TableNumber.Contains(term)));
        }

        // Recompute the row total in memory (matches the list's Total column; Order.GrandTotal
        // is not EF-translatable). One pass feeds counts, active count, and the revenue tiles.
        var rows = await query
            .Select(x => new
            {
                x.Order.Status,
                x.Order.IsPaid,
                Subtotal = x.Order.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                x.Order.DiscountAmount,
                x.Order.RoundingAdjustment,
                x.Order.Currency
            })
            .ToListAsync(cancellationToken);

        decimal Total(decimal subtotal, decimal discount, decimal rounding)
            => Math.Max(0m, subtotal - discount + rounding);

        var statusCounts = new Dictionary<string, int>();
        foreach (var name in Enum.GetNames<OrderStatus>())
            statusCounts[name] = 0;
        foreach (var r in rows)
            statusCounts[r.Status.ToString()]++;

        var active = rows.Count(r => r.Status != OrderStatus.Completed && r.Status != OrderStatus.Cancelled);

        var paidRevenue = rows
            .Where(r => r.IsPaid && r.Status != OrderStatus.Cancelled)
            .Sum(r => Total(r.Subtotal, r.DiscountAmount, r.RoundingAdjustment));

        var outstanding = rows
            .Where(r => !r.IsPaid && r.Status != OrderStatus.Cancelled)
            .Sum(r => Total(r.Subtotal, r.DiscountAmount, r.RoundingAdjustment));

        var currency = rows.Select(r => r.Currency).FirstOrDefault() ?? "Tk";

        return new OrdersSummaryDto(rows.Count, active, paidRevenue, outstanding, currency, statusCounts);
    }
}
