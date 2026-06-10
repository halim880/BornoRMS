using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

public class ProductCategory : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ProductCategory() { }

    public static ProductCategory Create(string name, int displayOrder = 0, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));

        return new ProductCategory
        {
            Name = name.Trim(),
            DisplayOrder = displayOrder,
            Description = Trim(description),
            IsActive = true
        };
    }

    public void UpdateDetails(string name, int displayOrder, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));
        Name = name.Trim();
        DisplayOrder = displayOrder;
        Description = Trim(description);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
