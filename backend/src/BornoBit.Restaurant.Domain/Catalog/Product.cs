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

    /// <summary>Typical prep time in minutes; snapshotted onto the order line and used for the customer prep-ETA.</summary>
    public int PrepMinutes { get; private set; } = DefaultPrepMinutes;

    /// <summary>Fallback prep time used when a product has not been given an explicit value.</summary>
    public const int DefaultPrepMinutes = 10;

    /// <summary>How selling this product impacts stock. Defaults to <see cref="InventoryMethod.None"/> (no deduction).</summary>
    public InventoryMethod InventoryMethod { get; private set; } = InventoryMethod.None;

    /// <summary>True when this product is a combo / meal deal that bundles <see cref="ComboComponents"/> at the bundle price.</summary>
    public bool IsCombo { get; private set; }

    private readonly List<ProductVariant> _variants = new();
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    private readonly List<ProductOptionGroup> _optionGroups = new();
    public IReadOnlyCollection<ProductOptionGroup> OptionGroups => _optionGroups.AsReadOnly();

    private readonly List<ComboComponent> _comboComponents = new();
    public IReadOnlyCollection<ComboComponent> ComboComponents => _comboComponents.AsReadOnly();

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
        Guid? kitchenStationId = null,
        int prepMinutes = DefaultPrepMinutes)
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
            PrepMinutes = prepMinutes < 0 ? 0 : prepMinutes,
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
        Guid? kitchenStationId = null,
        int? prepMinutes = null)
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
        if (prepMinutes is { } pm) PrepMinutes = pm < 0 ? 0 : pm;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>Routes this product to a kitchen station (null = unassigned / "All").</summary>
    public void AssignStation(Guid? stationId) => KitchenStationId = stationId;

    /// <summary>Sets the typical prep time (minutes) used for the customer prep-ETA.</summary>
    public void SetPrepMinutes(int minutes) => PrepMinutes = minutes < 0 ? 0 : minutes;

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

    /// <summary>Flags this product as a combo (or back to a normal product).</summary>
    public void SetCombo(bool isCombo) => IsCombo = isCombo;

    // ── Modifier / add-on option groups ───────────────────────────────────────────────

    public ProductOptionGroup AddOptionGroup(string name, int minSelections, int maxSelections, int displayOrder = 0, string? banglaName = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Option group name is required.", nameof(name));
        if (minSelections < 0) throw new ArgumentOutOfRangeException(nameof(minSelections));
        if (maxSelections < 1) throw new ArgumentOutOfRangeException(nameof(maxSelections), "A group must allow at least one selection.");
        if (minSelections > maxSelections) throw new ArgumentException("Min selections cannot exceed max selections.", nameof(minSelections));

        var group = new ProductOptionGroup
        {
            ProductId = Id,
            Name = name.Trim(),
            BanglaName = Trim(banglaName),
            MinSelections = minSelections,
            MaxSelections = maxSelections,
            DisplayOrder = displayOrder,
            IsActive = true
        };
        _optionGroups.Add(group);
        return group;
    }

    /// <summary>
    /// Reconciles the option-group tree (groups + their options) with the desired state.
    /// Requires OptionGroups (and each group's Options) to be loaded.
    /// </summary>
    public void SyncOptionGroups(IReadOnlyList<OptionGroupSpec> desired)
    {
        var keepGroupIds = new HashSet<Guid>();
        foreach (var row in desired)
        {
            if (string.IsNullOrWhiteSpace(row.Name)) throw new ArgumentException("Option group name is required.");
            if (row.MaxSelections < 1) throw new ArgumentOutOfRangeException(nameof(desired), "A group must allow at least one selection.");
            if (row.MinSelections < 0 || row.MinSelections > row.MaxSelections)
                throw new ArgumentException("Invalid min/max selections.");

            var existing = row.Id is { } id ? _optionGroups.FirstOrDefault(g => g.Id == id) : null;
            if (existing is null)
            {
                existing = AddOptionGroup(row.Name, row.MinSelections, row.MaxSelections, row.DisplayOrder, row.BanglaName);
            }
            else
            {
                existing.Name = row.Name.Trim();
                existing.BanglaName = Trim(row.BanglaName);
                existing.MinSelections = row.MinSelections;
                existing.MaxSelections = row.MaxSelections;
                existing.DisplayOrder = row.DisplayOrder;
            }
            existing.SyncOptions(row.Options);
            keepGroupIds.Add(existing.Id);
        }
        _optionGroups.RemoveAll(g => !keepGroupIds.Contains(g.Id));
    }

    // ── Combo components ──────────────────────────────────────────────────────────────

    public ComboComponent AddComboComponent(Guid componentProductId, int quantity, int displayOrder = 0)
    {
        if (componentProductId == Guid.Empty) throw new ArgumentException("Component product is required.", nameof(componentProductId));
        if (componentProductId == Id) throw new ArgumentException("A combo cannot contain itself.", nameof(componentProductId));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));

        var component = new ComboComponent
        {
            ComboProductId = Id,
            ComponentProductId = componentProductId,
            Quantity = quantity,
            DisplayOrder = displayOrder
        };
        _comboComponents.Add(component);
        return component;
    }

    /// <summary>Reconciles the combo component list with the desired state. Requires ComboComponents loaded.</summary>
    public void SyncComboComponents(IReadOnlyList<(Guid? Id, Guid ComponentProductId, int Quantity, int DisplayOrder)> desired)
    {
        var keepIds = new HashSet<Guid>();
        foreach (var row in desired)
        {
            var existing = row.Id is { } id ? _comboComponents.FirstOrDefault(c => c.Id == id) : null;
            if (existing is not null)
            {
                if (row.ComponentProductId == Id) throw new ArgumentException("A combo cannot contain itself.");
                if (row.Quantity < 1) throw new ArgumentOutOfRangeException(nameof(desired));
                existing.ComponentProductId = row.ComponentProductId;
                existing.Quantity = row.Quantity;
                existing.DisplayOrder = row.DisplayOrder;
                keepIds.Add(existing.Id);
            }
            else
            {
                keepIds.Add(AddComboComponent(row.ComponentProductId, row.Quantity, row.DisplayOrder).Id);
            }
        }
        _comboComponents.RemoveAll(c => !keepIds.Contains(c.Id));
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
