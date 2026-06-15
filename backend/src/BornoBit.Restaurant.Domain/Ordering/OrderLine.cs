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

    /// <summary>Prep time in minutes, snapshotted from the product at order time. Backs the customer prep-ETA.</summary>
    public int PrepMinutes { get; set; }

    // VAT snapshots stamped at settlement (see SettleOrderCommand). Frozen so the VAT report stays
    // accurate even if a category's tax rate changes later. All zero on un-settled orders.
    /// <summary>The VAT rate (%) applied to this line at settlement.</summary>
    public decimal TaxRatePercentSnapshot { get; set; }
    /// <summary>The taxable base this line's VAT was computed on (post order-level discount).</summary>
    public decimal TaxableAmountSnapshot { get; set; }
    /// <summary>The VAT amount this line contributed at settlement.</summary>
    public decimal TaxAmountSnapshot { get; set; }

    private readonly List<OrderLineModifier> _modifiers = new();
    /// <summary>Modifiers / add-ons chosen for this line, with frozen price deltas.</summary>
    public IReadOnlyCollection<OrderLineModifier> Modifiers => _modifiers.AsReadOnly();

    /// <summary>Per-unit surcharge from all chosen add-ons (0 when none / free modifiers only).</summary>
    public decimal ModifiersTotal => _modifiers.Sum(m => m.PriceDelta);

    /// <summary>Effective per-unit price including add-on surcharges.</summary>
    public decimal EffectiveUnitPrice => UnitPriceSnapshot + ModifiersTotal;

    public decimal LineTotal => EffectiveUnitPrice * Quantity;

    /// <summary>Snapshots a chosen modifier / add-on onto this line. Price delta is frozen.</summary>
    public OrderLineModifier AddModifier(Guid? optionId, string groupName, string optionName, decimal priceDelta)
    {
        if (string.IsNullOrWhiteSpace(optionName)) throw new ArgumentException("Option name is required.", nameof(optionName));

        var modifier = new OrderLineModifier
        {
            OrderLineId = Id,
            OptionId = optionId,
            GroupName = string.IsNullOrWhiteSpace(groupName) ? string.Empty : groupName.Trim(),
            OptionName = optionName.Trim(),
            PriceDelta = priceDelta
        };
        _modifiers.Add(modifier);
        return modifier;
    }
}
