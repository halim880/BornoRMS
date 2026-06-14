using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Section-7 inventory alerts: low stock, out of stock, and today's top consumed items.</summary>
public record GetInventoryAlertsQuery(int TopConsumption = 8) : IRequest<InventoryAlertsDto>;

public record StockAlertRowDto(Guid Id, string Code, string Name, string UnitCode, decimal QtyOnHand, decimal ReorderLevel);
public record ConsumptionRowDto(string Code, string Name, string UnitCode, decimal QtyConsumed);

public record InventoryAlertsDto(
    IReadOnlyList<StockAlertRowDto> LowStock,
    IReadOnlyList<StockAlertRowDto> OutOfStock,
    IReadOnlyList<ConsumptionRowDto> TodaysConsumption);

public class GetInventoryAlertsQueryHandler : IRequestHandler<GetInventoryAlertsQuery, InventoryAlertsDto>
{
    private readonly IAppDbContext _db;

    public GetInventoryAlertsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<InventoryAlertsDto> Handle(GetInventoryAlertsQuery request, CancellationToken cancellationToken)
    {
        var todayStart = DateTime.UtcNow.Date;
        var tomorrow = todayStart.AddDays(1);

        var items = await (
            from i in _db.InventoryItems
            join u in _db.Units on i.BaseUnitId equals u.Id
            where i.IsActive
            select new { i.Id, i.Code, i.Name, UnitCode = u.Code, i.QtyOnHand, i.ReorderLevel })
            .ToListAsync(cancellationToken);

        var lowStock = items
            .Where(i => i.ReorderLevel > 0 && i.QtyOnHand > 0 && i.QtyOnHand <= i.ReorderLevel)
            .OrderBy(i => i.Name)
            .Select(i => new StockAlertRowDto(i.Id, i.Code, i.Name, i.UnitCode, i.QtyOnHand, i.ReorderLevel))
            .ToList();

        var outOfStock = items
            .Where(i => i.QtyOnHand <= 0)
            .OrderBy(i => i.Name)
            .Select(i => new StockAlertRowDto(i.Id, i.Code, i.Name, i.UnitCode, i.QtyOnHand, i.ReorderLevel))
            .ToList();

        var top = Math.Clamp(request.TopConsumption, 1, 50);
        var consumption = await (
            from m in _db.StockMovements
            join i in _db.InventoryItems on m.InventoryItemId equals i.Id
            join u in _db.Units on i.BaseUnitId equals u.Id
            where (m.MovementType == StockMovementType.ConsumptionOut || m.MovementType == StockMovementType.WastageOut)
                  && m.OccurredAtUtc >= todayStart && m.OccurredAtUtc < tomorrow
            select new { i.Code, i.Name, UnitCode = u.Code, m.QtyBase })
            .ToListAsync(cancellationToken);

        var todaysConsumption = consumption
            .GroupBy(c => new { c.Code, c.Name, c.UnitCode })
            .Select(g => new ConsumptionRowDto(g.Key.Code, g.Key.Name, g.Key.UnitCode, Math.Abs(g.Sum(x => x.QtyBase))))
            .OrderByDescending(r => r.QtyConsumed)
            .Take(top)
            .ToList();

        return new InventoryAlertsDto(lowStock, outOfStock, todaysConsumption);
    }
}
