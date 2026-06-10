using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>
/// Append-only ledger row: one entry for every change to an item's on-hand quantity.
/// <see cref="QtyBase"/> is signed (positive = stock in, negative = stock out) and expressed in the
/// item's base unit. <see cref="ReferenceType"/>/<see cref="ReferenceId"/> link back to the source
/// document (e.g. a GoodsReceipt or, in phase 2, an Order).
/// </summary>
public class StockMovement : AuditableEntity
{
    public Guid InventoryItemId { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public decimal QtyBase { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? Reason { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private StockMovement() { }

    public static StockMovement Create(
        Guid inventoryItemId,
        StockMovementType movementType,
        decimal qtyBase,
        DateTime occurredAtUtc,
        decimal unitCost = 0m,
        string? reason = null,
        string? referenceType = null,
        Guid? referenceId = null)
    {
        if (inventoryItemId == Guid.Empty) throw new ArgumentException("Item is required.", nameof(inventoryItemId));
        if (qtyBase == 0) throw new ArgumentOutOfRangeException(nameof(qtyBase), "Movement quantity cannot be zero.");

        return new StockMovement
        {
            InventoryItemId = inventoryItemId,
            MovementType = movementType,
            QtyBase = qtyBase,
            UnitCost = unitCost,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ReferenceType = string.IsNullOrWhiteSpace(referenceType) ? null : referenceType.Trim(),
            ReferenceId = referenceId,
            OccurredAtUtc = occurredAtUtc
        };
    }
}
