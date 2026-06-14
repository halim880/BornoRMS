using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Settings;

/// <summary>
/// Restaurant-wide billing defaults, edited at runtime from the staff Settings page (a single row).
/// The settlement engine prefills VAT and service charge from these; the cashier can override per order.
/// </summary>
public class RestaurantBillingSettings : AuditableEntity
{
    /// <summary>Default VAT rate (% of the post-discount taxable base).</summary>
    public decimal VatPercent { get; private set; }
    /// <summary>Default service charge rate (% of the post-discount taxable base).</summary>
    public decimal ServiceChargePercent { get; private set; }
    public string Currency { get; private set; } = "Tk";
    /// <summary>Whether the settlement screen exposes a tip field.</summary>
    public bool TipEnabled { get; private set; }
    /// <summary>Discounts at or above this percent require Manager/Admin approval.</summary>
    public decimal HighDiscountThresholdPercent { get; private set; }

    private RestaurantBillingSettings() { }

    public static RestaurantBillingSettings CreateDefault() => new()
    {
        VatPercent = 0m,
        ServiceChargePercent = 0m,
        Currency = "Tk",
        TipEnabled = true,
        HighDiscountThresholdPercent = 20m
    };

    public void Update(decimal vatPercent, decimal serviceChargePercent, string currency, bool tipEnabled, decimal highDiscountThresholdPercent)
    {
        if (vatPercent < 0m || vatPercent > 100m) throw new ArgumentOutOfRangeException(nameof(vatPercent));
        if (serviceChargePercent < 0m || serviceChargePercent > 100m) throw new ArgumentOutOfRangeException(nameof(serviceChargePercent));
        if (highDiscountThresholdPercent < 0m || highDiscountThresholdPercent > 100m) throw new ArgumentOutOfRangeException(nameof(highDiscountThresholdPercent));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        VatPercent = vatPercent;
        ServiceChargePercent = serviceChargePercent;
        Currency = currency.Trim();
        TipEnabled = tipEnabled;
        HighDiscountThresholdPercent = highDiscountThresholdPercent;
    }
}
