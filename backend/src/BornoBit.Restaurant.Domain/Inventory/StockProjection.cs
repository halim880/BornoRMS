namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>
/// Precomputed read-model of an item's on-hand balance, used for fast POS validation and dashboard
/// display without touching the ledger. Kept in lockstep with <see cref="InventoryItem.QtyOnHand"/>
/// by the application layer after every stock mutation; <see cref="StockMovement"/> remains the source
/// of truth. The PK is the <see cref="InventoryItemId"/> (1:1 with the item). Thin POCO — not audited,
/// not soft-deleted.
/// </summary>
public class StockProjection
{
    public Guid InventoryItemId { get; private set; }
    public decimal CurrentStock { get; private set; }
    public DateTime LastUpdatedUtc { get; private set; }

    private StockProjection() { }

    public static StockProjection Create(Guid inventoryItemId, decimal currentStock, DateTime utc)
    {
        if (inventoryItemId == Guid.Empty) throw new ArgumentException("Item is required.", nameof(inventoryItemId));
        return new StockProjection
        {
            InventoryItemId = inventoryItemId,
            CurrentStock = currentStock,
            LastUpdatedUtc = utc
        };
    }

    /// <summary>Sets the projected stock to the item's current on-hand cache (set-to-cache, never drifts).</summary>
    public void SetTo(decimal currentStock, DateTime utc)
    {
        CurrentStock = currentStock;
        LastUpdatedUtc = utc;
    }
}
