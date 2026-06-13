using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>Top-selling menu items by quantity over a date range (UTC), from paid orders.</summary>
public record GetTopSellingItemsQuery(DateTime From, DateTime To, int Top = 20)
    : IRequest<IReadOnlyList<TopItemRowDto>>;

public record TopItemRowDto(string Code, string Name, int QuantitySold, decimal Revenue, string Currency);

public class GetTopSellingItemsQueryHandler : IRequestHandler<GetTopSellingItemsQuery, IReadOnlyList<TopItemRowDto>>
{
    private readonly IAppDbContext _db;

    public GetTopSellingItemsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TopItemRowDto>> Handle(GetTopSellingItemsQuery request, CancellationToken cancellationToken)
    {
        var start = request.From.Date;
        var end = request.To.Date.AddDays(1);
        var top = Math.Clamp(request.Top, 1, 200);

        // Lines of paid, non-cancelled orders in range. Name/Code/Currency are snapshotted on the
        // line, so no MenuItem join is needed; group by the snapshot key (Code + Name).
        var lines = await _db.Orders
            .Where(o => o.IsPaid
                        && o.Status != OrderStatus.Cancelled
                        && o.PaidAtUtc != null
                        && o.PaidAtUtc >= start
                        && o.PaidAtUtc < end)
            .SelectMany(o => o.Lines.Select(l => new
            {
                l.Code,
                l.Name,
                l.Quantity,
                Revenue = l.UnitPriceSnapshot * l.Quantity,
                l.Currency
            }))
            .ToListAsync(cancellationToken);

        return lines
            .GroupBy(l => new { l.Code, l.Name })
            .Select(g => new TopItemRowDto(
                g.Key.Code,
                g.Key.Name,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.Revenue),
                g.Select(x => x.Currency).FirstOrDefault() ?? "Tk"))
            .OrderByDescending(r => r.QuantitySold)
            .ThenByDescending(r => r.Revenue)
            .Take(top)
            .ToList();
    }
}
