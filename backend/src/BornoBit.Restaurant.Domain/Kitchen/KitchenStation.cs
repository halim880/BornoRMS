using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Kitchen;

/// <summary>
/// A kitchen preparation station (e.g. Grill, Rice, Beverage). Products are routed to a station
/// so the Kitchen Display can show each station only the items it is responsible for.
/// The virtual "All" view is a UI concept (null filter), not a stored row.
/// </summary>
public class KitchenStation : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Code { get; private set; }
    /// <summary>Optional accent colour (e.g. "#0E7490") used for the station tab/column on the board.</summary>
    public string? ColorHex { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private KitchenStation() { }

    public static KitchenStation Create(string name, string? code = null, string? colorHex = null, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Station name is required.", nameof(name));

        return new KitchenStation
        {
            Name = name.Trim(),
            Code = Trim(code),
            ColorHex = Trim(colorHex),
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void UpdateDetails(string name, string? code, string? colorHex, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Station name is required.", nameof(name));
        Name = name.Trim();
        Code = Trim(code);
        ColorHex = Trim(colorHex);
        DisplayOrder = displayOrder;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
