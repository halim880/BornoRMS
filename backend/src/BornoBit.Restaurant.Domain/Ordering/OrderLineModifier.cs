using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Ordering;

/// <summary>
/// A modifier / add-on chosen for an order line, snapshotted at order time. Names and price are frozen
/// so the bill stays accurate even if the catalog option is later renamed, re-priced, or deleted.
/// <see cref="OptionId"/> is a loose back-reference (no FK) and may be null for legacy/free-text rows.
/// </summary>
public class OrderLineModifier : BaseEntity
{
    public Guid OrderLineId { get; set; }
    /// <summary>The catalog option this came from, if any. Loose reference — not enforced by FK.</summary>
    public Guid? OptionId { get; set; }
    public string GroupName { get; set; } = default!;
    public string OptionName { get; set; } = default!;
    /// <summary>Per-unit surcharge added to the line's unit price (0 for a free modifier).</summary>
    public decimal PriceDelta { get; set; }
}
