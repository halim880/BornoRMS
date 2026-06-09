using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

public record GetOrderQuery(Guid OrderId) : IRequest<OrderDetailDto>;

public record OrderLineDto(
    Guid MenuItemId,
    string Code,
    string Name,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerPhone,
    string? CustomerName,
    string? TableNumber,
    OrderType OrderType,
    OrderStatus Status,
    DateTime OrderedAtUtc,
    string Currency,
    string? Notes,
    decimal Subtotal,
    decimal DiscountAmount,
    string? DiscountReason,
    decimal GrandTotal,
    decimal Total,
    bool IsPaid,
    PaymentMethod? PaymentMethod,
    decimal? AmountTendered,
    decimal? ChangeGiven,
    IReadOnlyList<OrderLineDto> Lines);

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDetailDto>
{
    private readonly IAppDbContext _db;

    public GetOrderQueryHandler(IAppDbContext db) => _db = db;

    public async Task<OrderDetailDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var row = await (
            from o in _db.Orders
            join c in _db.Customers on o.CustomerId equals c.Id
            join t in _db.RestaurantTables on o.RestaurantTableId equals t.Id into tj
            from t in tj.DefaultIfEmpty()
            where o.Id == request.OrderId
            select new { Order = o, Customer = c, Table = t })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) throw new NotFoundException("Order not found.");

        var lines = await _db.OrderLines
            .Where(l => l.OrderId == request.OrderId)
            .Select(l => new OrderLineDto(l.MenuItemId, l.Code, l.Name, l.UnitPriceSnapshot, l.Quantity, l.UnitPriceSnapshot * l.Quantity))
            .ToListAsync(cancellationToken);

        var subtotal = lines.Sum(l => l.LineTotal);
        var grandTotal = Math.Max(0m, subtotal - row.Order.DiscountAmount);

        return new OrderDetailDto(
            row.Order.Id,
            row.Order.OrderNumber,
            row.Order.CustomerId,
            row.Customer.Phone,
            row.Customer.FullName,
            row.Table != null ? row.Table.TableNumber : null,
            row.Order.OrderType,
            row.Order.Status,
            row.Order.OrderedAtUtc,
            row.Order.Currency,
            row.Order.Notes,
            subtotal,
            row.Order.DiscountAmount,
            row.Order.DiscountReason,
            grandTotal,
            grandTotal,
            row.Order.IsPaid,
            row.Order.PaymentMethod,
            row.Order.AmountTendered,
            row.Order.ChangeGiven,
            lines);
    }
}
