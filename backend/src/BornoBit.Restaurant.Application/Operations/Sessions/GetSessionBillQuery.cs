using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>Full running bill for a session — every order's lines plus aggregated charges and balance due.</summary>
public record GetSessionBillQuery(Guid SessionId) : IRequest<SessionBillDto>;

public class GetSessionBillQueryHandler : IRequestHandler<GetSessionBillQuery, SessionBillDto>
{
    private readonly IAppDbContext _db;
    public GetSessionBillQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SessionBillDto> Handle(GetSessionBillQuery request, CancellationToken cancellationToken)
    {
        var header = await (
            from s in _db.DiningSessions
            join t in _db.RestaurantTables on s.RestaurantTableId equals t.Id
            where s.Id == request.SessionId
            select new { s.SessionNumber, t.TableNumber, s.GuestCount }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        var orders = await _db.Orders
            .Include(o => o.Lines)
            .Where(o => o.DiningSessionId == request.SessionId && o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.OrderedAtUtc)
            .ToListAsync(cancellationToken);

        var orderDtos = orders.Select(o => new SessionBillOrderDto(
            o.Id, o.OrderNumber, o.Status.ToString(), o.IsPaid, o.GrandTotal,
            o.Lines.Select(l => new SessionBillLineDto(l.Name, l.Quantity, l.UnitPriceSnapshot, l.LineTotal)).ToList()))
            .ToList();

        var subtotal = orders.Sum(o => o.Subtotal);
        var discount = orders.Sum(o => o.DiscountAmount);
        var tax = orders.Sum(o => o.TaxAmount);
        var service = orders.Sum(o => o.ServiceChargeAmount);
        var rounding = orders.Sum(o => o.RoundingAdjustment);
        var grand = orders.Sum(o => o.GrandTotal);
        var paid = orders.Where(o => o.IsPaid).Sum(o => o.GrandTotal);
        var currency = orders.Select(o => o.Currency).FirstOrDefault() ?? "Tk";

        return new SessionBillDto(request.SessionId, header.SessionNumber, header.TableNumber, header.GuestCount,
            orderDtos, subtotal, discount, tax, service, rounding, grand, paid, Math.Max(0m, grand - paid), currency);
    }
}
