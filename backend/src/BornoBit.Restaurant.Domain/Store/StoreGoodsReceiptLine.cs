using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>A single received item on a store goods receipt. <see cref="QtyBase"/> is the entered
/// quantity converted to the item's base unit (computed at the application layer from the unit factor).</summary>
public class StoreGoodsReceiptLine : BaseEntity
{
    public Guid StoreGoodsReceiptId { get; set; }
    public Guid StoreItemId { get; set; }
    public string ItemName { get; set; } = default!;
    public decimal Qty { get; set; }
    public Guid UnitId { get; set; }
    public decimal QtyBase { get; set; }
    public decimal UnitCost { get; set; }

    public decimal LineTotal => Qty * UnitCost;
}
