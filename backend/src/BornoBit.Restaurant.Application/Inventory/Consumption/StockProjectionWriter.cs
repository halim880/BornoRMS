using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Consumption;

/// <summary>
/// Keeps the <see cref="StockProjection"/> read model in lockstep with an item's on-hand cache.
/// Every ledger writer (consumption, goods-receipt post, adjustment, wastage) calls
/// <see cref="BumpAsync"/> after mutating the item, passing the item's post-mutation
/// <c>QtyOnHand</c>. Setting the projection to the current cache (not applying a delta) means the
/// two can never drift, even if a delta were miscomputed. Does not call SaveChanges — the caller owns it.
/// </summary>
public static class StockProjectionWriter
{
    public static async Task BumpAsync(IAppDbContext db, Guid inventoryItemId, decimal newQtyOnHand, DateTime utc, CancellationToken ct)
    {
        var projection = await db.StockProjections
            .FirstOrDefaultAsync(p => p.InventoryItemId == inventoryItemId, ct);

        if (projection is null)
            db.StockProjections.Add(StockProjection.Create(inventoryItemId, newQtyOnHand, utc));
        else
            projection.SetTo(newQtyOnHand, utc);
    }
}
