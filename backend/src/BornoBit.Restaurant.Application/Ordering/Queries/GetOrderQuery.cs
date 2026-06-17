using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

public record GetOrderQuery(Guid OrderId) : IRequest<OrderDetailDto>;

public record OrderLineModifierDto(string GroupName, string OptionName, decimal PriceDelta, Guid? OptionId = null);

public record OrderLineDto(
    Guid MenuItemId,
    Guid? VariantId,
    string Code,
    string Name,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? Notes = null,
    IReadOnlyList<OrderLineModifierDto>? Modifiers = null,
    string? ImagePath = null);

public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerPhone,
    string? CustomerName,
    string? CustomerAddress,
    string? TableNumber,
    OrderType OrderType,
    OrderStatus Status,
    DateTime OrderedAtUtc,
    string Currency,
    string? Notes,
    decimal Subtotal,
    decimal DiscountAmount,
    string? DiscountReason,
    decimal TaxAmount,
    decimal ServiceChargeAmount,
    decimal TipAmount,
    decimal RoundingAdjustment,
    decimal GrandTotal,
    decimal Total,
    bool IsPaid,
    PaymentStatus PaymentStatus,
    decimal AmountPaid,
    decimal BalanceDue,
    PaymentMethod? PaymentMethod,
    decimal? AmountTendered,
    decimal? ChangeGiven,
    DateTime? PaidAtUtc,
    string? WaiterName,
    Guid? DiningSessionId,
    DateTime? ConfirmedAtUtc,
    DateTime? EstimatedReadyAtUtc,
    IReadOnlyList<OrderLineDto> Lines,
    IReadOnlyList<PaymentLineDto> Payments);

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
            .Select(l => new OrderLineDto(
                l.MenuItemId, l.VariantId, l.Code, l.Name, l.UnitPriceSnapshot, l.Quantity,
                (l.UnitPriceSnapshot + l.Modifiers.Sum(m => m.PriceDelta)) * l.Quantity,
                l.Notes,
                l.Modifiers
                    .OrderBy(m => m.GroupName)
                    .Select(m => new OrderLineModifierDto(m.GroupName, m.OptionName, m.PriceDelta, m.OptionId))
                    .ToList(),
                _db.Products.Where(p => p.Id == l.MenuItemId).Select(p => p.ImagePath).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var payments = await _db.Payments
            .Where(p => p.OrderId == request.OrderId)
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => new PaymentLineDto(p.Id, p.Method, p.Provider, p.Amount, p.Tendered, p.Change, p.Kind, p.Status, p.CreatedAtUtc, p.CashierName, p.Reference))
            .ToListAsync(cancellationToken);

        var ord = row.Order;
        var subtotal = lines.Sum(l => l.LineTotal);
        var grandTotal = Math.Max(0m, subtotal - ord.DiscountAmount + ord.TaxAmount + ord.ServiceChargeAmount + ord.TipAmount + ord.RoundingAdjustment);
        var amountPaid = payments
            .Where(p => p.Status == PaymentEntryStatus.Captured)
            .Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount);
        var balanceDue = Math.Max(0m, grandTotal - amountPaid);

        return new OrderDetailDto(
            ord.Id,
            ord.OrderNumber,
            ord.CustomerId,
            row.Customer.Phone,
            row.Customer.FullName,
            row.Customer.Address,
            row.Table != null ? row.Table.TableNumber : null,
            ord.OrderType,
            ord.Status,
            ord.OrderedAtUtc,
            ord.Currency,
            ord.Notes,
            subtotal,
            ord.DiscountAmount,
            ord.DiscountReason,
            ord.TaxAmount,
            ord.ServiceChargeAmount,
            ord.TipAmount,
            ord.RoundingAdjustment,
            grandTotal,
            grandTotal,
            ord.IsPaid,
            ord.PaymentStatus,
            amountPaid,
            balanceDue,
            ord.PaymentMethod,
            ord.AmountTendered,
            ord.ChangeGiven,
            ord.PaidAtUtc,
            ord.WaiterName,
            ord.DiningSessionId,
            ord.ConfirmedAtUtc,
            ord.EstimatedReadyAtUtc,
            lines,
            payments);
    }
}
