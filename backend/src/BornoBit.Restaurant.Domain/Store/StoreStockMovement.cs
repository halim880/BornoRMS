using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>
/// Append-only ledger row for the store/warehouse: one entry for every change to an item's on-hand quantity.
/// <see cref="QtyBase"/> is signed (positive = stock in, negative = stock out) and expressed in the item's
/// base unit. <see cref="ReferenceType"/>/<see cref="ReferenceId"/> link back to the source document
/// (a StoreGoodsReceipt or a StoreIssue).
/// </summary>
public class StoreStockMovement : AuditableEntity
{
    public Guid StoreItemId { get; private set; }
    public StoreMovementType MovementType { get; private set; }
    public decimal QtyBase { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? Reason { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private StoreStockMovement() { }

    public static StoreStockMovement Create(
        Guid storeItemId,
        StoreMovementType movementType,
        decimal qtyBase,
        DateTime occurredAtUtc,
        decimal unitCost = 0m,
        string? reason = null,
        string? referenceType = null,
        Guid? referenceId = null)
    {
        if (storeItemId == Guid.Empty) throw new ArgumentException("Item is required.", nameof(storeItemId));
        if (qtyBase == 0) throw new ArgumentOutOfRangeException(nameof(qtyBase), "Movement quantity cannot be zero.");

        return new StoreStockMovement
        {
            StoreItemId = storeItemId,
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
