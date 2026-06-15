using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Consumption;

/// <inheritdoc />
public class StockConsumptionService : IStockConsumptionService
{
    private const string OrderRef = "Order";
    private readonly TimeProvider _time;

    public StockConsumptionService(TimeProvider time) => _time = time;

    public async Task ApplyConsumptionAsync(IAppDbContext db, Order order, CancellationToken ct)
    {
        // Idempotency: already handled, or a prior run already wrote consumption rows for this order.
        if (order.StockSyncStatus is StockSyncStatus.Synced or StockSyncStatus.Reversed) return;

        var alreadyConsumed = await db.StockMovements.AnyAsync(
            m => m.ReferenceType == OrderRef && m.ReferenceId == order.Id && m.MovementType == StockMovementType.ConsumptionOut, ct);
        if (alreadyConsumed)
        {
            order.MarkStockSynced();
            return;
        }

        var lines = order.Lines
            .Select(l => new RecipeExploder.LineInput(l.MenuItemId, l.VariantId, l.Quantity))
            .ToList();

        var requirements = await RecipeExploder.ExplodeAsync(db, lines, ct);
        if (requirements.Count == 0)
        {
            // Nothing in this order impacts stock (all None, or no recipe/link configured).
            order.MarkStockSynced();
            return;
        }

        var itemIds = requirements.Select(r => r.InventoryItemId).ToList();
        var items = await db.InventoryItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var reason = $"Order {order.OrderNumber}";

        foreach (var req in requirements)
        {
            if (!items.TryGetValue(req.InventoryItemId, out var item)) continue; // orphaned recipe ingredient — skip

            item.Consume(req.QtyBase);

            db.StockMovements.Add(StockMovement.Create(
                item.Id,
                StockMovementType.ConsumptionOut,
                qtyBase: -req.QtyBase,
                occurredAtUtc: nowUtc,
                unitCost: item.AvgCost,
                reason: reason,
                referenceType: OrderRef,
                referenceId: order.Id));

            await StockProjectionWriter.BumpAsync(db, item.Id, item.QtyOnHand, nowUtc, ct);
        }

        order.MarkStockSynced();
    }

    public async Task ReverseConsumptionAsync(IAppDbContext db, Order order, CancellationToken ct)
    {
        if (order.StockSyncStatus == StockSyncStatus.Reversed) return;

        // Net this order's deductions (ConsumptionOut, stored negative) against any earlier line-void
        // restores (AdjustmentIn, positive) so we only give back what is still outstanding — restoring
        // the full ConsumptionOut would double-restore a line that was already voided back.
        var movements = await db.StockMovements
            .Where(m => m.ReferenceType == OrderRef && m.ReferenceId == order.Id
                        && (m.MovementType == StockMovementType.ConsumptionOut || m.MovementType == StockMovementType.AdjustmentIn))
            .ToListAsync(ct);
        if (movements.Count == 0) return;

        var perItem = movements
            .GroupBy(m => m.InventoryItemId)
            .Select(g => new { ItemId = g.Key, Outstanding = -g.Sum(m => m.QtyBase) })
            .Where(x => x.Outstanding > 0m)
            .ToList();

        var itemIds = perItem.Select(x => x.ItemId).ToList();
        var items = await db.InventoryItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var reason = $"Order {order.OrderNumber} reversed";

        foreach (var row in perItem)
        {
            if (!items.TryGetValue(row.ItemId, out var item)) continue;

            item.RestoreConsumed(row.Outstanding);

            db.StockMovements.Add(StockMovement.Create(
                item.Id,
                StockMovementType.AdjustmentIn,
                qtyBase: row.Outstanding,
                occurredAtUtc: nowUtc,
                unitCost: item.AvgCost,
                reason: reason,
                referenceType: OrderRef,
                referenceId: order.Id));

            await StockProjectionWriter.BumpAsync(db, item.Id, item.QtyOnHand, nowUtc, ct);
        }

        order.MarkStockReversed();
    }

    public async Task ReverseLineAsync(IAppDbContext db, Order order, OrderLine line, CancellationToken ct)
    {
        var input = new[] { new RecipeExploder.LineInput(line.MenuItemId, line.VariantId, line.Quantity) };
        var requirements = await RecipeExploder.ExplodeAsync(db, input, ct);
        if (requirements.Count == 0) return; // line has no stock impact

        var itemIds = requirements.Select(r => r.InventoryItemId).ToList();
        var items = await db.InventoryItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var reason = $"Order {order.OrderNumber} item void";

        foreach (var req in requirements)
        {
            if (req.QtyBase <= 0m || !items.TryGetValue(req.InventoryItemId, out var item)) continue;

            item.RestoreConsumed(req.QtyBase);

            db.StockMovements.Add(StockMovement.Create(
                item.Id,
                StockMovementType.AdjustmentIn,
                qtyBase: req.QtyBase,
                occurredAtUtc: nowUtc,
                unitCost: item.AvgCost,
                reason: reason,
                referenceType: OrderRef,
                referenceId: order.Id));

            await StockProjectionWriter.BumpAsync(db, item.Id, item.QtyOnHand, nowUtc, ct);
        }
    }
}
