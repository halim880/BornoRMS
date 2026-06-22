using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Kitchen;

/// <summary>
/// A physical kitchen (e.g. Main Kitchen, Bar, Bakery) that groups one or more <see cref="KitchenStation"/>s.
/// Each kitchen has its own Kitchen Display screen and its own printer, so an order spanning two kitchens
/// prints a separate kitchen ticket (KOT) per kitchen. A line's kitchen is derived transitively from its
/// station; lines whose station has no kitchen (or no station at all) fall back to the <see cref="IsDefault"/> kitchen.
/// </summary>
public class Kitchen : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Code { get; private set; }
    /// <summary>Optional accent colour (e.g. "#0E7490") used for the kitchen tab/selector on the board.</summary>
    public string? ColorHex { get; private set; }
    /// <summary>Printer this kitchen's KOT is sent to. Null ⇒ fall back to the global KOT printer.</summary>
    public string? PrinterName { get; private set; }
    public int DisplayOrder { get; private set; }
    /// <summary>The catch-all kitchen for lines whose station has no kitchen (or no station). Exactly one should be true.</summary>
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Kitchen() { }

    public static Kitchen Create(string name, string? code = null, string? colorHex = null, string? printerName = null, int displayOrder = 0, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Kitchen name is required.", nameof(name));

        return new Kitchen
        {
            Name = name.Trim(),
            Code = Trim(code),
            ColorHex = Trim(colorHex),
            PrinterName = Trim(printerName),
            DisplayOrder = displayOrder,
            IsDefault = isDefault,
            IsActive = true
        };
    }

    public void UpdateDetails(string name, string? code, string? colorHex, string? printerName, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Kitchen name is required.", nameof(name));
        Name = name.Trim();
        Code = Trim(code);
        ColorHex = Trim(colorHex);
        PrinterName = Trim(printerName);
        DisplayOrder = displayOrder;
    }

    public void SetDefault(bool isDefault) => IsDefault = isDefault;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
