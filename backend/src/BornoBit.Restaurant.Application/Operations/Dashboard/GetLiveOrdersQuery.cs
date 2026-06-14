using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Section-4 live order monitor: latest orders, optionally filtered by status, server-side paged.</summary>
public record GetLiveOrdersQuery(OrderStatus? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<LiveOrderRowDto>>;

public record LiveOrderRowDto(
    Guid Id,
    string OrderNumber,
    string? TableNumber,
    OrderType OrderType,
    string Channel,
    DateTime OrderedAtUtc,
    decimal Total,
    OrderStatus Status,
    bool IsPaid,
    string Currency);

public class GetLiveOrdersQueryHandler : IRequestHandler<GetLiveOrdersQuery, PagedResult<LiveOrderRowDto>>
{
    private readonly IAppDbContext _db;

    public GetLiveOrdersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<LiveOrderRowDto>> Handle(GetLiveOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query =
            from o in _db.Orders
            join t in _db.RestaurantTables on o.RestaurantTableId equals t.Id into tj
            from t in tj.DefaultIfEmpty()
            select new { Order = o, Table = t };

        if (request.Status is { } st)
            query = query.Where(x => x.Order.Status == st);

        var total = await query.LongCountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.Order.OrderedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Order.Id,
                x.Order.OrderNumber,
                TableNumber = x.Table != null ? x.Table.TableNumber : null,
                x.Order.OrderType,
                x.Order.WaiterUserId,
                x.Order.OrderedAtUtc,
                Subtotal = x.Order.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                x.Order.DiscountAmount,
                x.Order.TaxAmount,
                x.Order.ServiceChargeAmount,
                x.Order.RoundingAdjustment,
                x.Order.Status,
                x.Order.IsPaid,
                x.Order.Currency
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new LiveOrderRowDto(
            x.Id,
            x.OrderNumber,
            x.TableNumber,
            x.OrderType,
            x.WaiterUserId == null ? "QR" : "Staff",
            x.OrderedAtUtc,
            Math.Max(0m, x.Subtotal - x.DiscountAmount + x.TaxAmount + x.ServiceChargeAmount + x.RoundingAdjustment),
            x.Status,
            x.IsPaid,
            x.Currency)).ToList();

        return new PagedResult<LiveOrderRowDto>(items, page, pageSize, total);
    }
}
