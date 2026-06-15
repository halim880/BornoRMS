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
}
