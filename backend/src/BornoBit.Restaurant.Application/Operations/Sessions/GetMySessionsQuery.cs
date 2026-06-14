using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>Active sessions (Open/Billing). Defaults to the current waiter's own sessions.</summary>
public record GetMySessionsQuery(bool OnlyMine = true) : IRequest<IReadOnlyList<SessionRowDto>>;

public class GetMySessionsQueryHandler : IRequestHandler<GetMySessionsQuery, IReadOnlyList<SessionRowDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMySessionsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SessionRowDto>> Handle(GetMySessionsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var query =
            from s in _db.DiningSessions
            join t in _db.RestaurantTables on s.RestaurantTableId equals t.Id
            where s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing
            select new { Session = s, t.TableNumber };

        if (request.OnlyMine && _currentUser.UserId is { } uid)
            query = query.Where(x => x.Session.WaiterUserId == uid);

        var sessions = await query
            .OrderBy(x => x.Session.OpenedAtUtc)
            .Select(x => new
            {
                x.Session.Id,
                x.Session.SessionNumber,
                x.Session.RestaurantTableId,
                x.TableNumber,
                x.Session.GuestCount,
                x.Session.WaiterUserId,
                x.Session.WaiterName,
                x.Session.Status,
                x.Session.OpenedAtUtc
            })
            .ToListAsync(cancellationToken);

        var sessionIds = sessions.Select(s => s.Id).ToList();
        var orders = await _db.Orders
            .Where(o => o.DiningSessionId != null && sessionIds.Contains(o.DiningSessionId.Value)
                        && o.Status != OrderStatus.Cancelled)
            .Select(o => new
            {
                SessionId = o.DiningSessionId!.Value,
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.RoundingAdjustment,
                o.Currency
            })
            .ToListAsync(cancellationToken);

        var bySession = orders.GroupBy(o => o.SessionId).ToDictionary(g => g.Key, g => g.ToList());

        return sessions.Select(s =>
        {
            bySession.TryGetValue(s.Id, out var os);
            os ??= new();
            var bill = os.Sum(o => Math.Max(0m, o.Subtotal - o.DiscountAmount + o.TaxAmount + o.ServiceChargeAmount + o.RoundingAdjustment));
            var currency = os.Select(o => o.Currency).FirstOrDefault() ?? "Tk";
            var minutes = (int)Math.Max(0, (now - s.OpenedAtUtc).TotalMinutes);
            return new SessionRowDto(s.Id, s.SessionNumber, s.RestaurantTableId, s.TableNumber, s.GuestCount,
                s.WaiterUserId, s.WaiterName, s.Status, s.OpenedAtUtc, minutes, os.Count, bill, currency);
        }).ToList();
    }
}
