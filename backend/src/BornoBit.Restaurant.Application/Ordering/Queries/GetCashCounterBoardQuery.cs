using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>
/// Paged, filterable cash-counter board — every order with its live payment state and balance due.
/// Totals are recomputed in SQL including tax, service charge and tip; amount paid nets captured
/// charges against refunds. Server-side paged for 500+ orders/day.
/// </summary>
public record GetCashCounterBoardQuery(
    DateOnly? Date = null,
    Guid? TableId = null,
    string? Waiter = null,
    OrderType? OrderType = null,
    PaymentStatus? PaymentStatus = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<CashCounterRowDto>>;

public record CashCounterRowDto(
    Guid OrderId,
    string OrderNumber,
    string? TableNumber,
    Guid? DiningSessionId,
    string? WaiterName,
    OrderType OrderType,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal BalanceDue,
    int ItemCount,
    DateTime OrderedAtUtc);

public class GetCashCounterBoardQueryHandler : IRequestHandler<GetCashCounterBoardQuery, PagedResult<CashCounterRowDto>>
{
    private readonly IAppDbContext _db;
    public GetCashCounterBoardQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<CashCounterRowDto>> Handle(GetCashCounterBoardQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.Orders.Where(o => o.Status != OrderStatus.Cancelled);

        if (request.Date is { } date)
        {
            var from = date.ToDateTime(TimeOnly.MinValue);
            var to = from.AddDays(1);
            query = query.Where(o => o.OrderedAtUtc >= from && o.OrderedAtUtc < to);
        }
        if (request.TableId is { } tableId) query = query.Where(o => o.RestaurantTableId == tableId);
        if (!string.IsNullOrWhiteSpace(request.Waiter))
            query = query.Where(o => o.WaiterName != null && o.WaiterName.Contains(request.Waiter));
        if (request.OrderType is { } type) query = query.Where(o => o.OrderType == type);
        if (request.PaymentStatus is { } status) query = query.Where(o => o.PaymentStatus == status);

        var total = await query.LongCountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(o => o.OrderedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                TableNumber = _db.RestaurantTables.Where(t => t.Id == o.RestaurantTableId).Select(t => t.TableNumber).FirstOrDefault(),
                o.DiningSessionId,
                o.WaiterName,
                o.OrderType,
                o.Status,
                o.PaymentStatus,
                Subtotal = o.Lines.Sum(l => l.UnitPriceSnapshot * l.Quantity),
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.TipAmount,
                o.RoundingAdjustment,
                ItemCount = o.Lines.Count(),
                Paid = _db.Payments
                    .Where(p => p.OrderId == o.Id && p.Status == PaymentEntryStatus.Captured)
                    .Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount),
                o.OrderedAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r =>
        {
            var grand = Math.Max(0m, r.Subtotal - r.DiscountAmount + r.TaxAmount + r.ServiceChargeAmount + r.TipAmount + r.RoundingAdjustment);
            var balance = Math.Max(0m, grand - r.Paid);
            return new CashCounterRowDto(r.Id, r.OrderNumber, r.TableNumber, r.DiningSessionId, r.WaiterName,
                r.OrderType, r.Status, r.PaymentStatus, grand, r.Paid, balance, r.ItemCount, r.OrderedAtUtc);
        }).ToList();

        return new PagedResult<CashCounterRowDto>(items, page, pageSize, total);
    }
}
