using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>A single received item on a goods receipt. <see cref="QtyBase"/> is the entered
/// quantity converted to the item's base unit (computed at the application layer from the unit factor).</summary>
public class GoodsReceiptLine : BaseEntity
{
    public Guid GoodsReceiptId { get; set; }
    public Guid InventoryItemId { get; set; }
    public string ItemName { get; set; } = default!;
    public decimal Qty { get; set; }
    public Guid UnitId { get; set; }
    public decimal QtyBase { get; set; }
    public decimal UnitCost { get; set; }

    /// <summary>When this receipt is raised against a purchase order, the PO line this row fulfils (drives PO/GRN matching).</summary>
    public Guid? PurchaseOrderLineId { get; set; }

    public decimal LineTotal => Qty * UnitCost;
}
