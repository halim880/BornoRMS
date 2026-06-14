using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Dashboard;

/// <summary>
/// Waste ratio = |WastageOut| / (|WastageOut| + |ConsumptionOut|) over a window, overall and per item.
/// Measures how much of what left the shelf was spoilage versus genuine sale consumption.
/// </summary>
public record GetWastePercentQuery(int Days = 30, int Top = 10) : IRequest<WastePercentDto>;

public record WasteRow(Guid ItemId, string Code, string Name, decimal Wasted, decimal Consumed, decimal WastePercent);
public record WastePercentDto(decimal OverallPercent, decimal TotalWasted, decimal TotalConsumed, IReadOnlyList<WasteRow> ByItem);

public class GetWastePercentQueryHandler : IRequestHandler<GetWastePercentQuery, WastePercentDto>
{
    private readonly IAppDbContext _db;

    public GetWastePercentQueryHandler(IAppDbContext db) => _db = db;

    public async Task<WastePercentDto> Handle(GetWastePercentQuery request, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.Date.AddDays(-Math.Max(1, request.Days));
        var top = Math.Clamp(request.Top, 1, 50);

        var rows = await (
            from m in _db.StockMovements
            join i in _db.InventoryItems on m.InventoryItemId equals i.Id
            where (m.MovementType == StockMovementType.WastageOut || m.MovementType == StockMovementType.ConsumptionOut)
                  && m.OccurredAtUtc >= since
            select new { i.Id, i.Code, i.Name, m.MovementType, m.QtyBase })
            .ToListAsync(cancellationToken);

        static decimal Pct(decimal wasted, decimal consumed) => wasted + consumed <= 0m ? 0m : Math.Round(wasted / (wasted + consumed) * 100m, 1);

        var byItem = rows
            .GroupBy(r => new { r.Id, r.Code, r.Name })
            .Select(g =>
            {
                var wasted = Math.Abs(g.Where(x => x.MovementType == StockMovementType.WastageOut).Sum(x => x.QtyBase));
                var consumed = Math.Abs(g.Where(x => x.MovementType == StockMovementType.ConsumptionOut).Sum(x => x.QtyBase));
                return new WasteRow(g.Key.Id, g.Key.Code, g.Key.Name, wasted, consumed, Pct(wasted, consumed));
            })
            .OrderByDescending(r => r.WastePercent)
            .Take(top)
            .ToList();

        var totalWasted = Math.Abs(rows.Where(r => r.MovementType == StockMovementType.WastageOut).Sum(r => r.QtyBase));
        var totalConsumed = Math.Abs(rows.Where(r => r.MovementType == StockMovementType.ConsumptionOut).Sum(r => r.QtyBase));

        return new WastePercentDto(Pct(totalWasted, totalConsumed), totalWasted, totalConsumed, byItem);
    }
}
