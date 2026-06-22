using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Consumption;

/// <summary>
/// Pre-confirm stock check: explodes an order's lines through the same resolver the consumption engine
/// uses and reports any ingredient/direct-stock shortfall. Advisory only — never throws, never blocks.
/// </summary>
public record GetOrderStockAvailabilityQuery(Guid OrderId) : IRequest<OrderStockAvailabilityDto>;

public record StockShortageRow(
    Guid InventoryItemId, string Code, string Name, string UnitCode,
    decimal Required, decimal Available, decimal Shortfall);

public record OrderStockAvailabilityDto(bool HasShortages, IReadOnlyList<StockShortageRow> Shortages);

public class GetOrderStockAvailabilityQueryHandler : IRequestHandler<GetOrderStockAvailabilityQuery, OrderStockAvailabilityDto>
{
    private static readonly OrderStockAvailabilityDto Empty = new(false, Array.Empty<StockShortageRow>());
    private readonly IAppDbContext _db;

    public GetOrderStockAvailabilityQueryHandler(IAppDbContext db) => _db = db;

    public async Task<OrderStockAvailabilityDto> Handle(GetOrderStockAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Lines).ThenInclude(l => l.Modifiers)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        var lines = order.Lines
            .Select(l => new RecipeExploder.LineInput(l.MenuItemId, l.VariantId, l.Quantity))
            .ToList();

        var modifiers = order.Lines
            .SelectMany(l => l.Modifiers
                .Where(m => m.OptionId != null)
                .Select(m => new RecipeExploder.ModifierInput(m.OptionId!.Value, l.Quantity)))
            .ToList();

        var requirements = await RecipeExploder.ExplodeAsync(_db, lines, modifiers, cancellationToken);
        if (requirements.Count == 0) return Empty;

        var itemIds = requirements.Select(r => r.InventoryItemId).ToList();

        // Available = projection where present, else the item's on-hand cache.
        var info = await (
            from i in _db.InventoryItems
            join u in _db.Units on i.BaseUnitId equals u.Id
            where itemIds.Contains(i.Id)
            join p in _db.StockProjections on i.Id equals p.InventoryItemId into pj
            from p in pj.DefaultIfEmpty()
            select new { i.Id, i.Code, i.Name, UnitCode = u.Code, i.QtyOnHand, Projected = (decimal?)(p == null ? null : p.CurrentStock) })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var shortages = new List<StockShortageRow>();
        foreach (var req in requirements)
        {
            if (!info.TryGetValue(req.InventoryItemId, out var x)) continue;
            var available = x.Projected ?? x.QtyOnHand;
            if (available < req.QtyBase)
                shortages.Add(new StockShortageRow(x.Id, x.Code, x.Name, x.UnitCode, req.QtyBase, available, req.QtyBase - available));
        }

        return new OrderStockAvailabilityDto(shortages.Count > 0, shortages);
    }
}
