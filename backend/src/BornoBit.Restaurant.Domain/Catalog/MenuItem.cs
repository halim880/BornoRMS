using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

public class MenuItem : AuditableEntity
{
    public Guid MenuCategoryId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "Tk";
    public string? ImageUrl { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    private MenuItem() { }

    public static MenuItem Create(
        Guid menuCategoryId,
        string code,
        string name,
        decimal price,
        string currency = "Tk",
        string? description = null,
        string? imageUrl = null,
        bool isAvailable = true,
        int displayOrder = 0)
    {
        if (menuCategoryId == Guid.Empty) throw new ArgumentException("Category is required.", nameof(menuCategoryId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        return new MenuItem
        {
            MenuCategoryId = menuCategoryId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Price = price,
            Currency = currency.Trim(),
            Description = Trim(description),
            ImageUrl = Trim(imageUrl),
            IsAvailable = isAvailable,
            DisplayOrder = displayOrder
        };
    }

    public void UpdateDetails(string name, decimal price, string currency, string? description, string? imageUrl, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        Name = name.Trim();
        Price = price;
        Currency = currency.Trim();
        Description = Trim(description);
        ImageUrl = Trim(imageUrl);
        DisplayOrder = displayOrder;
    }

    public void SetAvailability(bool available) => IsAvailable = available;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
