using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Section-2 live floor: every active table with its derived status, guests, session age and bill.</summary>
public record GetTableOverviewQuery : IRequest<IReadOnlyList<TableOverviewRowDto>>;

public record TableOverviewRowDto(
    Guid TableId,
    string TableNumber,
    int Capacity,
    DerivedTableStatus Status,
    int? GuestCount,
    DateTime? SessionStartUtc,
    int? SessionMinutes,
    decimal CurrentBill,
    Guid? OrderId,
    string? OrderNumber,
    string Currency,
    Guid? SessionId,
    int OrderCount,
    string? WaiterName);

public class GetTableOverviewQueryHandler : IRequestHandler<GetTableOverviewQuery, IReadOnlyList<TableOverviewRowDto>>
{
    private readonly IAppDbContext _db;

    public GetTableOverviewQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TableOverviewRowDto>> Handle(GetTableOverviewQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrow = todayStart.AddDays(1);

        var tables = await _db.RestaurantTables
            .Where(t => t.IsActive)
            .OrderBy(t => t.TableNumber)
            .Select(t => new { t.Id, t.TableNumber, t.Capacity })
            .ToListAsync(cancellationToken);

        // Active sessions drive table status now (one active session per table).
        var sessions = await _db.DiningSessions
            .Where(s => s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing)
            .Select(s => new { s.Id, s.RestaurantTableId, s.GuestCount, s.OpenedAtUtc, s.WaiterName, s.Status })
            .ToListAsync(cancellationToken);
        var sessionByTable = sessions
            .GroupBy(s => s.RestaurantTableId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.OpenedAtUtc).First());

        var sessionIds = sessions.Select(s => s.Id).ToList();

        // Orders attached to those sessions — for running bill, order count, latest order, served-unpaid flag.
        var orders = await _db.Orders
            .Where(o => o.DiningSessionId != null && sessionIds.Contains(o.DiningSessionId.Value)
                        && o.Status != OrderStatus.Cancelled)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                SessionId = o.DiningSessionId!.Value,
                o.Status,
                o.IsPaid,
                o.OrderedAtUtc,
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.RoundingAdjustment,
                o.Currency
            })
            .ToListAsync(cancellationToken);
        var ordersBySession = orders.GroupBy(o => o.SessionId).ToDictionary(g => g.Key, g => g.ToList());

        var bookedTableIds = await _db.TableReservations
            .Where(r => r.Status == ReservationStatus.Booked
                        && r.ReservedForUtc >= todayStart && r.ReservedForUtc < tomorrow)
            .Select(r => r.RestaurantTableId).Distinct().ToListAsync(cancellationToken);
        var booked = bookedTableIds.ToHashSet();

        var rows = new List<TableOverviewRowDto>(tables.Count);
        foreach (var t in tables)
        {
            if (sessionByTable.TryGetValue(t.Id, out var session))
            {
                ordersBySession.TryGetValue(session.Id, out var os);
                os ??= new();

                var bill = os.Sum(o => Math.Max(0m,
                    o.Subtotal - o.DiscountAmount + o.TaxAmount + o.ServiceChargeAmount + o.RoundingAdjustment));
                var hasServedUnpaid = os.Any(o => o.Status == OrderStatus.Served && !o.IsPaid);
                var status = session.Status == DiningSessionStatus.Billing || hasServedUnpaid
                    ? DerivedTableStatus.WaitingPayment
                    : DerivedTableStatus.Occupied;
                var latest = os.OrderByDescending(o => o.OrderedAtUtc).FirstOrDefault();
                var minutes = (int)Math.Max(0, (now - session.OpenedAtUtc).TotalMinutes);
                var currency = os.Select(o => o.Currency).FirstOrDefault() ?? "Tk";

                rows.Add(new TableOverviewRowDto(
                    t.Id, t.TableNumber, t.Capacity, status, session.GuestCount,
                    session.OpenedAtUtc, minutes, bill, latest?.Id, latest?.OrderNumber, currency,
                    session.Id, os.Count, session.WaiterName));
            }
            else
            {
                var status = booked.Contains(t.Id) ? DerivedTableStatus.Reserved : DerivedTableStatus.Available;
                rows.Add(new TableOverviewRowDto(
                    t.Id, t.TableNumber, t.Capacity, status, null, null, null, 0m, null, null, "Tk",
                    null, 0, null));
            }
        }

        return rows;
    }
}
