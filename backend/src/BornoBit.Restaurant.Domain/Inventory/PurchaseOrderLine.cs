using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>
/// A single ordered item on a purchase order. <see cref="QtyOrderedBase"/> is the ordered quantity converted
/// to the item's base unit (computed at the application layer). <see cref="QtyReceivedBase"/> is a running
/// tally bumped each time a goods receipt is posted against this line — the basis for PO/GRN matching.
/// </summary>
public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid InventoryItemId { get; set; }
    public string ItemName { get; set; } = default!;
    public decimal QtyOrdered { get; set; }
    public Guid UnitId { get; set; }
    public decimal QtyOrderedBase { get; set; }
    public decimal UnitCost { get; set; }

    /// <summary>Cumulative received quantity (in base units) from posted goods receipts matched to this line.</summary>
    public decimal QtyReceivedBase { get; set; }

    public decimal LineTotal => QtyOrdered * UnitCost;

    /// <summary>Base-unit quantity still to be received. Clamped at zero (over-receipt is allowed but never negative-outstanding).</summary>
    public decimal OutstandingBase => Math.Max(0m, QtyOrderedBase - QtyReceivedBase);

    public bool IsFullyReceived => QtyReceivedBase >= QtyOrderedBase;
}
