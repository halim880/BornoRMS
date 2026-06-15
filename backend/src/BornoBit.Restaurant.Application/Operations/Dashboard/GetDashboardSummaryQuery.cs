using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>The Section-1 KPI cards: today's sales, table occupancy, kitchen load, customer activity.</summary>
public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public record DashboardSummaryDto(
    // Today's sales
    decimal TodayRevenue,
    int TodayOrderCount,
    decimal AverageOrderValue,
    // Tables
    int OccupiedTables,
    int AvailableTables,
    int ReservedTables,
    int WaitingPaymentTables,
    // Kitchen
    int PendingOrders,
    int PreparingOrders,
    int ReadyOrders,
    // Customer activity
    int ActiveDiningSessions,
    int QrOrdersToday,
    int WalkInOrdersToday,
    string Currency);

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetDashboardSummaryQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var (todayStart, tomorrow) = _clock.DayWindowUtc(_clock.Today);

        // Today's paid takings (recompute total in memory — Order.GrandTotal is not EF-translatable).
        var todayPaid = await _db.Orders
            .Where(o => o.IsPaid && o.Status != OrderStatus.Cancelled
                        && o.PaidAtUtc != null && o.PaidAtUtc >= todayStart && o.PaidAtUtc < tomorrow)
            .Select(o => new
            {
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.RoundingAdjustment,
                o.Currency
            })
            .ToListAsync(cancellationToken);

        var todayRevenue = todayPaid.Sum(x => Math.Max(0m,
            x.Subtotal - x.DiscountAmount + x.TaxAmount + x.ServiceChargeAmount + x.RoundingAdjustment));
        var todayCount = todayPaid.Count;
        var aov = todayCount > 0 ? Math.Round(todayRevenue / todayCount, 2) : 0m;
        var currency = todayPaid.Select(x => x.Currency).FirstOrDefault() ?? "Tk";

        // Open orders (not paid, not cancelled/completed) — feed table occupancy + kitchen load.
        var openOrders = await _db.Orders
            .Where(o => !o.IsPaid && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Completed)
            .Select(o => new { o.RestaurantTableId, o.DiningSessionId, o.Status, o.OrderType })
            .ToListAsync(cancellationToken);

        var activeTableCount = await _db.RestaurantTables.CountAsync(t => t.IsActive, cancellationToken);

        // Table occupancy is derived from active dining sessions (one per table).
        var activeSessions = await _db.DiningSessions
            .Where(s => s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing)
            .Select(s => new { s.Id, s.RestaurantTableId, s.Status })
            .ToListAsync(cancellationToken);

        var servedUnpaidSessionIds = openOrders
            .Where(o => o.DiningSessionId != null && o.Status == OrderStatus.Served)
            .Select(o => o.DiningSessionId!.Value).Distinct().ToHashSet();

        var waitingPaymentTableIds = activeSessions
            .Where(s => s.Status == DiningSessionStatus.Billing || servedUnpaidSessionIds.Contains(s.Id))
            .Select(s => s.RestaurantTableId).Distinct().ToHashSet();
        var occupiedTableIds = activeSessions
            .Select(s => s.RestaurantTableId).Distinct()
            .Where(id => !waitingPaymentTableIds.Contains(id)).ToHashSet();

        var reservedTableIds = await _db.TableReservations
            .Where(r => r.Status == ReservationStatus.Booked
                        && r.ReservedForUtc >= todayStart && r.ReservedForUtc < tomorrow)
            .Select(r => r.RestaurantTableId).Distinct().ToListAsync(cancellationToken);
        var reservedCount = reservedTableIds
            .Where(id => !occupiedTableIds.Contains(id) && !waitingPaymentTableIds.Contains(id)).Distinct().Count();

        var occupied = occupiedTableIds.Count;
        var waiting = waitingPaymentTableIds.Count;
        var available = Math.Max(0, activeTableCount - occupied - waiting - reservedCount);

        // Kitchen load.
        var pending = openOrders.Count(o => o.Status is OrderStatus.Placed or OrderStatus.Confirmed);
        var preparing = openOrders.Count(o => o.Status == OrderStatus.Preparing);
        var ready = openOrders.Count(o => o.Status == OrderStatus.Ready);

        var activeDiningSessions = activeSessions.Count;

        // Channel split is a proxy: self-service (QR/customer site) orders carry no waiter; staff entry does.
        var todayOrders = await _db.Orders
            .Where(o => o.Status != OrderStatus.Cancelled
                        && o.OrderedAtUtc >= todayStart && o.OrderedAtUtc < tomorrow)
            .Select(o => new { o.WaiterUserId })
            .ToListAsync(cancellationToken);
        var qrToday = todayOrders.Count(o => o.WaiterUserId == null);
        var walkInToday = todayOrders.Count(o => o.WaiterUserId != null);

        return new DashboardSummaryDto(
            todayRevenue, todayCount, aov,
            occupied, available, reservedCount, waiting,
            pending, preparing, ready,
            activeDiningSessions, qrToday, walkInToday,
            currency);
    }
}
