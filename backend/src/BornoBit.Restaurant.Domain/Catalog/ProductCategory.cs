using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

public class ProductCategory : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>VAT rate (%) for items in this category. Null inherits the restaurant default VAT;
    /// an explicit value (including 0 for exempt) overrides it.</summary>
    public decimal? TaxRatePercent { get; private set; }

    private ProductCategory() { }

    public static ProductCategory Create(string name, int displayOrder = 0, string? description = null, decimal? taxRatePercent = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));
        ValidateRate(taxRatePercent);

        return new ProductCategory
        {
            Name = name.Trim(),
            DisplayOrder = displayOrder,
            Description = Trim(description),
            TaxRatePercent = taxRatePercent,
            IsActive = true
        };
    }

    public void UpdateDetails(string name, int displayOrder, string? description, decimal? taxRatePercent = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));
        ValidateRate(taxRatePercent);
        Name = name.Trim();
        DisplayOrder = displayOrder;
        Description = Trim(description);
        TaxRatePercent = taxRatePercent;
    }

    private static void ValidateRate(decimal? rate)
    {
        if (rate is { } r && (r < 0m || r > 100m))
            throw new ArgumentOutOfRangeException(nameof(rate), "Tax rate must be between 0 and 100.");
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
