using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>A single issued item on a store issue. <see cref="QtyBase"/> is the entered quantity
/// converted to the item's base unit (computed at the application layer from the unit factor).</summary>
public class StoreIssueLine : BaseEntity
{
    public Guid StoreIssueId { get; set; }
    public Guid StoreItemId { get; set; }
    public string ItemName { get; set; } = default!;
    public decimal Qty { get; set; }
    public Guid UnitId { get; set; }
    public decimal QtyBase { get; set; }
}
