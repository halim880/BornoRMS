using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

public record GetOrdersQuery(
    OrderStatus? Status = null,
    Guid? CustomerId = null,
    int Page = 1,
    int PageSize = 50,
    bool? IsPaid = null,
    bool ExcludeCancelled = false,
    // Optional list scoping (back-office Orders screen): date window on OrderedAtUtc,
    // a combined free-text search, and an exact-ish order-number lookup.
    DateOnly? From = null,
    DateOnly? To = null,
    string? Search = null,
    string? OrderNumber = null
) : IRequest<PagedResult<OrderListItemDto>>;

public record OrderListItemDto(
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
    int ItemCount,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    bool IsPaid,
    DateTime? PaidAtUtc = null,
    PaymentMethod? PaymentMethod = null,
    DateTime? ConfirmedAtUtc = null,
    DateTime? EstimatedReadyAtUtc = null);

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetOrdersQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<OrderListItemDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query =
            from o in _db.Orders
            join c in _db.Customers on o.CustomerId equals c.Id
            join t in _db.RestaurantTables on o.RestaurantTableId equals t.Id into tj
            from t in tj.DefaultIfEmpty()
            select new { Order = o, Customer = c, Table = t };

        if (request.Status is { } st)
            query = query.Where(x => x.Order.Status == st);

        if (request.CustomerId is { } cid)
            query = query.Where(x => x.Order.CustomerId == cid);

        if (request.IsPaid is { } paid)
            query = query.Where(x => x.Order.IsPaid == paid);

        if (request.ExcludeCancelled)
            query = query.Where(x => x.Order.Status != OrderStatus.Cancelled);

        if (request.From is { } from)
        {
            var (start, _) = _clock.DayWindowUtc(from);
            query = query.Where(x => x.Order.OrderedAtUtc >= start);
        }

        if (request.To is { } to)
        {
            var (_, end) = _clock.DayWindowUtc(to);
            query = query.Where(x => x.Order.OrderedAtUtc < end);
        }

        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
        {
            var on = request.OrderNumber.Trim().ToUpper();
            query = query.Where(x => x.Order.OrderNumber.Contains(on));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.Order.OrderNumber.Contains(term)
                || (x.Customer.FullName != null && x.Customer.FullName.Contains(term))
                || x.Customer.Phone.Contains(term)
                || (x.Table != null && x.Table.TableNumber.Contains(term)));
        }

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Order.OrderedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OrderListItemDto(
                x.Order.Id,
                x.Order.OrderNumber,
                x.Order.CustomerId,
                x.Customer.Phone,
                x.Customer.FullName,
                x.Table != null ? x.Table.TableNumber : null,
                x.Order.OrderType,
                x.Order.Status,
                x.Order.OrderedAtUtc,
                x.Order.Currency,
                x.Order.Lines.Count(),
                x.Order.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                x.Order.DiscountAmount,
                (x.Order.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m) - x.Order.DiscountAmount + x.Order.RoundingAdjustment,
                x.Order.IsPaid,
                x.Order.PaidAtUtc,
                x.Order.PaymentMethod,
                x.Order.ConfirmedAtUtc,
                x.Order.EstimatedReadyAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderListItemDto>(items, page, pageSize, total);
    }
}
