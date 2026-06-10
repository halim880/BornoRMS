using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>Grouping for stock items (e.g. চাল/ডাল, মাংস, মাছ, মশলা).</summary>
public class InventoryCategory : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? BanglaName { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private InventoryCategory() { }

    public static InventoryCategory Create(string name, int displayOrder = 0, string? banglaName = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));

        return new InventoryCategory
        {
            Name = name.Trim(),
            BanglaName = Trim(banglaName),
            Description = Trim(description),
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void UpdateDetails(string name, int displayOrder, string? banglaName, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));
        Name = name.Trim();
        DisplayOrder = displayOrder;
        BanglaName = Trim(banglaName);
        Description = Trim(description);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
