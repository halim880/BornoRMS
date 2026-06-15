using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Section-8 staff leaderboard: today's orders, tables and revenue per waiter.</summary>
public record GetStaffActivityQuery : IRequest<IReadOnlyList<StaffActivityRowDto>>;

public record StaffActivityRowDto(
    string WaiterName,
    int OrdersProcessed,
    int TablesAssigned,
    decimal Revenue);

public class GetStaffActivityQueryHandler : IRequestHandler<GetStaffActivityQuery, IReadOnlyList<StaffActivityRowDto>>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetStaffActivityQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<StaffActivityRowDto>> Handle(GetStaffActivityQuery request, CancellationToken cancellationToken)
    {
        var (todayStart, tomorrow) = _clock.DayWindowUtc(_clock.Today);

        var orders = await _db.Orders
            .Where(o => o.WaiterName != null && o.Status != OrderStatus.Cancelled
                        && o.OrderedAtUtc >= todayStart && o.OrderedAtUtc < tomorrow)
            .Select(o => new
            {
                o.WaiterName,
                o.RestaurantTableId,
                o.IsPaid,
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.RoundingAdjustment
            })
            .ToListAsync(cancellationToken);

        return orders
            .GroupBy(o => o.WaiterName!)
            .Select(g => new StaffActivityRowDto(
                g.Key,
                g.Count(),
                g.Where(x => x.RestaurantTableId != null).Select(x => x.RestaurantTableId).Distinct().Count(),
                g.Where(x => x.IsPaid).Sum(x => Math.Max(0m,
                    x.Subtotal - x.DiscountAmount + x.TaxAmount + x.ServiceChargeAmount + x.RoundingAdjustment))))
            .OrderByDescending(r => r.Revenue)
            .ThenByDescending(r => r.OrdersProcessed)
            .ToList();
    }
}
