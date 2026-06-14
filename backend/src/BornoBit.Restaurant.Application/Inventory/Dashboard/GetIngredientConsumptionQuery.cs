using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Dashboard;

/// <summary>Most-consumed ingredients over a window (from order ConsumptionOut movements), with cost value.</summary>
public record GetIngredientConsumptionQuery(int Days = 30, int Top = 10) : IRequest<IReadOnlyList<IngredientConsumptionRow>>;

public record IngredientConsumptionRow(Guid ItemId, string Code, string Name, string UnitCode, decimal QtyConsumed, decimal Value);

public class GetIngredientConsumptionQueryHandler
    : IRequestHandler<GetIngredientConsumptionQuery, IReadOnlyList<IngredientConsumptionRow>>
{
    private readonly IAppDbContext _db;

    public GetIngredientConsumptionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<IngredientConsumptionRow>> Handle(GetIngredientConsumptionQuery request, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.Date.AddDays(-Math.Max(1, request.Days));
        var top = Math.Clamp(request.Top, 1, 50);

        var rows = await (
            from m in _db.StockMovements
            join i in _db.InventoryItems on m.InventoryItemId equals i.Id
            join u in _db.Units on i.BaseUnitId equals u.Id
            where m.MovementType == StockMovementType.ConsumptionOut && m.OccurredAtUtc >= since
            select new { i.Id, i.Code, i.Name, UnitCode = u.Code, m.QtyBase, m.UnitCost })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => new { r.Id, r.Code, r.Name, r.UnitCode })
            .Select(g => new IngredientConsumptionRow(
                g.Key.Id, g.Key.Code, g.Key.Name, g.Key.UnitCode,
                Math.Abs(g.Sum(x => x.QtyBase)),
                Math.Abs(g.Sum(x => x.QtyBase * x.UnitCost))))
            .OrderByDescending(r => r.QtyConsumed)
            .Take(top)
            .ToList();
    }
}
