using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

/// <summary>
/// A single choice inside a <see cref="ProductOptionGroup"/> — e.g. "Mild" in a Spice group,
/// or "Extra cheese (+50)" in an Add-ons group. <see cref="PriceDelta"/> is added to the line's
/// unit price; zero means a free modifier (e.g. "No onion").
/// </summary>
public class ProductOption : BaseEntity
{
    public Guid OptionGroupId { get; set; }
    public string Name { get; set; } = default!;
    public string? BanglaName { get; set; }
    public decimal PriceDelta { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional inventory item this add-on consumes when chosen (e.g. "Extra Meat" → a Meat item).
    /// Null ⇒ the add-on has no stock impact (e.g. "No onion"). <see cref="ConsumeQtyBase"/> is the
    /// amount to deduct per unit of the order line, expressed in the item's base unit.
    /// </summary>
    public Guid? InventoryItemId { get; set; }
    public decimal ConsumeQtyBase { get; set; }
}
