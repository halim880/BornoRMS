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

    /// <summary>Kitchen station this product is routed to on the Kitchen Display. Null = unassigned ("All").</summary>
    public Guid? KitchenStationId { get; private set; }

    /// <summary>How selling this product impacts stock. Defaults to <see cref="InventoryMethod.None"/> (no deduction).</summary>
    public InventoryMethod InventoryMethod { get; private set; } = InventoryMethod.None;

    private readonly List<ProductVariant> _variants = new();
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

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
        string currency = "Tk",
        Guid? kitchenStationId = null)
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
            KitchenStationId = kitchenStationId,
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
        int displayOrder,
        Guid? kitchenStationId = null)
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
        KitchenStationId = kitchenStationId;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>Routes this product to a kitchen station (null = unassigned / "All").</summary>
    public void AssignStation(Guid? stationId) => KitchenStationId = stationId;

    /// <summary>Sets how selling this product impacts stock (None / DirectStock / RecipeBased).</summary>
    public void SetInventoryMethod(InventoryMethod method) => InventoryMethod = method;

    public ProductVariant AddVariant(string name, decimal price, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Variant name is required.", nameof(name));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");

        var variant = new ProductVariant
        {
            ProductId = Id,
            Name = name.Trim(),
            Price = price,
            DisplayOrder = displayOrder,
            IsActive = true
        };
        _variants.Add(variant);
        return variant;
    }

    /// <summary>
    /// Reconciles the variant list with the desired state: rows with a matching Id are updated,
    /// rows without an Id are added, and existing variants not present are removed.
    /// Requires the Variants collection to be loaded.
    /// </summary>
    public void SyncVariants(IReadOnlyList<(Guid? Id, string Name, decimal Price, int DisplayOrder)> desired)
    {
        var keepIds = new HashSet<Guid>();

        foreach (var row in desired)
        {
            if (string.IsNullOrWhiteSpace(row.Name)) throw new ArgumentException("Variant name is required.");
            if (row.Price < 0) throw new ArgumentOutOfRangeException(nameof(desired), "Variant price cannot be negative.");

            var existing = row.Id is { } id ? _variants.FirstOrDefault(v => v.Id == id) : null;
            if (existing is not null)
            {
                existing.Name = row.Name.Trim();
                existing.Price = row.Price;
                existing.DisplayOrder = row.DisplayOrder;
                keepIds.Add(existing.Id);
            }
            else
            {
                keepIds.Add(AddVariant(row.Name, row.Price, row.DisplayOrder).Id);
            }
        }

        _variants.RemoveAll(v => !keepIds.Contains(v.Id));
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
