using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>The waiter console's top-strip widget counters, scoped to the current waiter where relevant.</summary>
public record GetWaiterDashboardQuery : IRequest<WaiterDashboardDto>;

public class GetWaiterDashboardQueryHandler : IRequestHandler<GetWaiterDashboardQuery, WaiterDashboardDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBusinessClock _clock;

    public GetWaiterDashboardQueryHandler(IAppDbContext db, ICurrentUser currentUser, IBusinessClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<WaiterDashboardDto> Handle(GetWaiterDashboardQuery request, CancellationToken cancellationToken)
    {
        var (todayStart, tomorrow) = _clock.DayWindowUtc(_clock.Today);
        var me = _currentUser.UserId;

        var activeSessions = await _db.DiningSessions
            .Where(s => s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing)
            .Select(s => new { s.Id, s.RestaurantTableId, s.WaiterUserId, s.Status })
            .ToListAsync(cancellationToken);

        var activeTableCount = await _db.RestaurantTables.CountAsync(t => t.IsActive, cancellationToken);

        var occupiedTables = activeSessions.Select(s => s.RestaurantTableId).Distinct().Count();
        var available = Math.Max(0, activeTableCount - occupiedTables);
        var myActive = me is { } uid ? activeSessions.Count(s => s.WaiterUserId == uid) : 0;
        var myTables = me is { } uid2 ? activeSessions.Where(s => s.WaiterUserId == uid2).Select(s => s.RestaurantTableId).Distinct().Count() : 0;

        var pendingRequests = await _db.CustomerRequests.CountAsync(r => r.Status == CustomerRequestStatus.Pending, cancellationToken);
        var readyOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Ready, cancellationToken);

        // Sessions awaiting payment: explicitly in Billing, or holding a served-but-unpaid order.
        var billingSessionIds = activeSessions.Where(s => s.Status == DiningSessionStatus.Billing).Select(s => s.Id).ToHashSet();
        var servedUnpaidSessionIds = await _db.Orders
            .Where(o => o.DiningSessionId != null && o.Status == OrderStatus.Served && !o.IsPaid)
            .Select(o => o.DiningSessionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        billingSessionIds.UnionWith(servedUnpaidSessionIds);
        var billsWaiting = billingSessionIds.Count;

        // My revenue served today (paid orders attributed to me).
        var myPaidToday = me is { } uid3
            ? await _db.Orders
                .Where(o => o.WaiterUserId == uid3 && o.IsPaid && o.PaidAtUtc != null
                            && o.PaidAtUtc >= todayStart && o.PaidAtUtc < tomorrow)
                .Select(o => new
                {
                    Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                    o.DiscountAmount, o.TaxAmount, o.ServiceChargeAmount, o.RoundingAdjustment, o.Currency
                })
                .ToListAsync(cancellationToken)
            : new();

        var myRevenue = myPaidToday.Sum(o => Math.Max(0m, o.Subtotal - o.DiscountAmount + o.TaxAmount + o.ServiceChargeAmount + o.RoundingAdjustment));
        var currency = myPaidToday.Select(o => o.Currency).FirstOrDefault() ?? "Tk";

        return new WaiterDashboardDto(
            myTables, available, occupiedTables, pendingRequests, readyOrders, billsWaiting, myActive, myRevenue, currency);
    }
}
