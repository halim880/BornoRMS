using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

/// <summary>
/// A sellable variation of a product — e.g. a grill in Quarter/Half/Full, or a drink in
/// Regular/Large. Each variant carries its own price; the parent product's price acts as the
/// price when no variants exist.
/// </summary>
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
