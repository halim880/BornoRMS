using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Ordering;

public class OrderLine : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    /// <summary>Product variant chosen for this line (waiter flow), if any.</summary>
    public Guid? VariantId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal UnitPriceSnapshot { get; set; }
    public string Currency { get; set; } = "Tk";
    public int Quantity { get; set; } = 1;

    /// <summary>Kitchen station this line is routed to, snapshotted from the product at order time.</summary>
    public Guid? StationId { get; set; }
    /// <summary>Denormalised station name so the Kitchen Display can render without a join.</summary>
    public string? StationName { get; set; }
    /// <summary>Per-line special instructions / modifiers (free text), e.g. "No onion", "Extra spicy".</summary>
    public string? Notes { get; set; }

    public decimal LineTotal => UnitPriceSnapshot * Quantity;
}
