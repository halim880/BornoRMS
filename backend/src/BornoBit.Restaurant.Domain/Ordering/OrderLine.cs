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

    public decimal LineTotal => UnitPriceSnapshot * Quantity;
}
