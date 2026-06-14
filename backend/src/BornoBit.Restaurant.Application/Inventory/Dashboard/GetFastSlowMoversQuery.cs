using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Dashboard;

/// <summary>Fastest- and slowest-moving items by consumed volume over a window (among active items).</summary>
public record GetFastSlowMoversQuery(int Days = 30, int Top = 5) : IRequest<FastSlowMoversDto>;

public record MoverRow(Guid ItemId, string Code, string Name, string UnitCode, decimal QtyConsumed);
public record FastSlowMoversDto(IReadOnlyList<MoverRow> Fast, IReadOnlyList<MoverRow> Slow);

public class GetFastSlowMoversQueryHandler : IRequestHandler<GetFastSlowMoversQuery, FastSlowMoversDto>
{
    private readonly IAppDbContext _db;

    public GetFastSlowMoversQueryHandler(IAppDbContext db) => _db = db;

    public async Task<FastSlowMoversDto> Handle(GetFastSlowMoversQuery request, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.Date.AddDays(-Math.Max(1, request.Days));
        var top = Math.Clamp(request.Top, 1, 50);

        // Every active item, with its consumption in the window (0 if none).
        var items = await (
            from i in _db.InventoryItems
            join u in _db.Units on i.BaseUnitId equals u.Id
            where i.IsActive
            select new { i.Id, i.Code, i.Name, UnitCode = u.Code })
            .ToListAsync(cancellationToken);

        var consumption = await (
            from m in _db.StockMovements
            where m.MovementType == StockMovementType.ConsumptionOut && m.OccurredAtUtc >= since
            group m by m.InventoryItemId into g
            select new { ItemId = g.Key, Qty = g.Sum(x => x.QtyBase) })
            .ToDictionaryAsync(x => x.ItemId, x => Math.Abs(x.Qty), cancellationToken);

        var ranked = items
            .Select(i => new MoverRow(i.Id, i.Code, i.Name, i.UnitCode, consumption.TryGetValue(i.Id, out var q) ? q : 0m))
            .ToList();

        var fast = ranked.Where(r => r.QtyConsumed > 0).OrderByDescending(r => r.QtyConsumed).Take(top).ToList();
        var slow = ranked.OrderBy(r => r.QtyConsumed).ThenBy(r => r.Name).Take(top).ToList();

        return new FastSlowMoversDto(fast, slow);
    }
}
