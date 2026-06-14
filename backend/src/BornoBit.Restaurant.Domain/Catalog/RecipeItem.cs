using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

/// <summary>
/// One ingredient line of a <see cref="Recipe"/>: <see cref="Quantity"/> of an <c>InventoryItem</c>
/// expressed in <see cref="UnitId"/>. Converted to the item's base unit (via <c>Unit.ToBase</c>) at
/// consume time. Reconciled through the parent's <see cref="Recipe.SyncItems"/>.
/// </summary>
public class RecipeItem : AuditableEntity
{
    public Guid RecipeId { get; internal set; }
    public Guid InventoryItemId { get; internal set; }
    /// <summary>Quantity in <see cref="UnitId"/> units (e.g. 250 g). Converted to base units when consumed.</summary>
    public decimal Quantity { get; internal set; }
    public Guid UnitId { get; internal set; }

    internal RecipeItem() { }
}
