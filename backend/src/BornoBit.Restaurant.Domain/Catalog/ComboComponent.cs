using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

/// <summary>
/// One member product of a combo / meal deal. The owning combo <see cref="Product"/> (IsCombo = true)
/// sells at its own bundle <see cref="Product.Price"/>; components drive what prints on the kitchen
/// ticket. <see cref="ComponentProductId"/> points at a normal sellable product.
/// </summary>
public class ComboComponent : BaseEntity
{
    /// <summary>The combo product that owns this component.</summary>
    public Guid ComboProductId { get; set; }
    /// <summary>The member product included in the combo.</summary>
    public Guid ComponentProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public int DisplayOrder { get; set; }
}
