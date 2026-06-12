using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Pos;

/// <summary>
/// Orders still open at the POS: not paid, not completed, not cancelled. Drives the order-chip
/// queue; oldest first so chips keep a stable position as new orders are appended.
/// </summary>
public record GetActiveOrdersQuery() : IRequest<IReadOnlyList<ActiveOrderDto>>;

public record ActiveOrderDto(
    Guid Id,
    string OrderNumber,
    OrderType OrderType,
    Guid? TableId,
    string? TableNumber,
    OrderStatus Status,
    DateTime OrderedAtUtc,
    int ItemCount,
    decimal Total,
    string Currency);

public class GetActiveOrdersQueryHandler : IRequestHandler<GetActiveOrdersQuery, IReadOnlyList<ActiveOrderDto>>
{
    private readonly IAppDbContext _db;

    public GetActiveOrdersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ActiveOrderDto>> Handle(GetActiveOrdersQuery request, CancellationToken cancellationToken)
    {
        return await (
            from o in _db.Orders
            join t in _db.RestaurantTables on o.RestaurantTableId equals t.Id into tj
            from t in tj.DefaultIfEmpty()
            where !o.IsPaid && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Completed
            orderby o.OrderedAtUtc
            select new ActiveOrderDto(
                o.Id,
                o.OrderNumber,
                o.OrderType,
                o.RestaurantTableId,
                t != null ? t.TableNumber : null,
                o.Status,
                o.OrderedAtUtc,
                o.Lines.Count(),
                (o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m) - o.DiscountAmount + o.RoundingAdjustment,
                o.Currency))
            .ToListAsync(cancellationToken);
    }
}
