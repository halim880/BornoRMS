using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

public class Product : AuditableEntity
{
    public Guid ProductCategoryId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? BanglaName { get; private set; }
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "Tk";
    public string? ImagePath { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Product() { }

    public static Product Create(
        Guid productCategoryId,
        string code,
        string name,
        decimal price,
        string? banglaName = null,
        string? imagePath = null,
        string? description = null,
        int displayOrder = 0,
        string currency = "Tk")
    {
        if (productCategoryId == Guid.Empty) throw new ArgumentException("Category is required.", nameof(productCategoryId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        return new Product
        {
            ProductCategoryId = productCategoryId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            BanglaName = Trim(banglaName),
            Price = price,
            Currency = currency.Trim(),
            ImagePath = Trim(imagePath),
            Description = Trim(description),
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void UpdateDetails(
        Guid productCategoryId,
        string code,
        string name,
        decimal price,
        string? banglaName,
        string? imagePath,
        string? description,
        int displayOrder)
    {
        if (productCategoryId == Guid.Empty) throw new ArgumentException("Category is required.", nameof(productCategoryId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));

        ProductCategoryId = productCategoryId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Price = price;
        BanglaName = Trim(banglaName);
        ImagePath = Trim(imagePath);
        Description = Trim(description);
        DisplayOrder = displayOrder;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
