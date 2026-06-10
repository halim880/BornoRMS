using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>
/// Unit of measure. Stock is stored in the base unit of each <see cref="Dimension"/>
/// (kg for Weight, litre for Volume, piece for Count); <see cref="ToBaseFactor"/> converts a
/// quantity entered in this unit into the base unit (qty × factor = base). BD units supported:
/// seer (0.9331 kg), mon (37.3242 kg = 40 seer), hali (4 pcs), dozen (12 pcs).
/// </summary>
public class Unit : AuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? BanglaName { get; private set; }
    public UnitDimension Dimension { get; private set; }
    public decimal ToBaseFactor { get; private set; } = 1m;
    public bool IsActive { get; private set; } = true;

    private Unit() { }

    public static Unit Create(
        string code,
        string name,
        UnitDimension dimension,
        decimal toBaseFactor,
        string? banglaName = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (toBaseFactor <= 0) throw new ArgumentOutOfRangeException(nameof(toBaseFactor), "Conversion factor must be positive.");

        return new Unit
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            BanglaName = Trim(banglaName),
            Dimension = dimension,
            ToBaseFactor = toBaseFactor,
            IsActive = true
        };
    }

    public void UpdateDetails(string name, decimal toBaseFactor, string? banglaName)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (toBaseFactor <= 0) throw new ArgumentOutOfRangeException(nameof(toBaseFactor));

        Name = name.Trim();
        ToBaseFactor = toBaseFactor;
        BanglaName = Trim(banglaName);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>Convert a quantity expressed in this unit to the dimension's base unit.</summary>
    public decimal ToBase(decimal qty) => qty * ToBaseFactor;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
