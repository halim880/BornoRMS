using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>A single dining session with its running totals.</summary>
public record GetSessionQuery(Guid SessionId) : IRequest<SessionRowDto>;

public class GetSessionQueryHandler : IRequestHandler<GetSessionQuery, SessionRowDto>
{
    private readonly IAppDbContext _db;
    public GetSessionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SessionRowDto> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var row = await (
            from s in _db.DiningSessions
            join t in _db.RestaurantTables on s.RestaurantTableId equals t.Id
            where s.Id == request.SessionId
            select new
            {
                s.Id, s.SessionNumber, s.RestaurantTableId, t.TableNumber, s.GuestCount,
                s.WaiterUserId, s.WaiterName, s.Status, s.OpenedAtUtc
            }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        var orders = await _db.Orders
            .Where(o => o.DiningSessionId == request.SessionId && o.Status != OrderStatus.Cancelled)
            .Select(o => new
            {
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount, o.TaxAmount, o.ServiceChargeAmount, o.RoundingAdjustment, o.Currency
            })
            .ToListAsync(cancellationToken);

        var bill = orders.Sum(o => Math.Max(0m, o.Subtotal - o.DiscountAmount + o.TaxAmount + o.ServiceChargeAmount + o.RoundingAdjustment));
        var currency = orders.Select(o => o.Currency).FirstOrDefault() ?? "Tk";
        var minutes = (int)Math.Max(0, (now - row.OpenedAtUtc).TotalMinutes);

        return new SessionRowDto(row.Id, row.SessionNumber, row.RestaurantTableId, row.TableNumber, row.GuestCount,
            row.WaiterUserId, row.WaiterName, row.Status, row.OpenedAtUtc, minutes, orders.Count, bill, currency);
    }
}
