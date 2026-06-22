using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

/// <summary>Desired state of one option group (with its options) for <see cref="Product.SyncOptionGroups"/>.</summary>
public record OptionGroupSpec(
    Guid? Id,
    string Name,
    string? BanglaName,
    int MinSelections,
    int MaxSelections,
    int DisplayOrder,
    IReadOnlyList<(Guid? Id, string Name, string? BanglaName, decimal PriceDelta, int DisplayOrder, Guid? InventoryItemId, decimal ConsumeQtyBase)> Options);

/// <summary>
/// A set of modifier / add-on choices attached to a product — e.g. "Spice level" (single, required)
/// or "Add-ons" (multi, optional). The selection rules are expressed by <see cref="MinSelections"/> /
/// <see cref="MaxSelections"/>: Max == 1 renders as a single-select (radio), Max &gt; 1 as multi-select
/// (checkboxes); Min &gt;= 1 makes the group required. Owned by a <see cref="Product"/> (mirrors
/// <see cref="ProductVariant"/>).
/// </summary>
public class ProductOptionGroup : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = default!;
    public string? BanglaName { get; set; }
    /// <summary>Minimum options the customer must pick (0 = optional). When &gt;= 1 the group is required.</summary>
    public int MinSelections { get; set; }
    /// <summary>Maximum options the customer may pick. 1 = single-select; &gt; 1 = multi-select.</summary>
    public int MaxSelections { get; set; } = 1;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    private readonly List<ProductOption> _options = new();
    public IReadOnlyCollection<ProductOption> Options => _options.AsReadOnly();

    /// <summary>True when at least one option must be picked.</summary>
    public bool IsRequired => MinSelections >= 1;
    /// <summary>True when only a single option may be picked (radio rather than checkboxes).</summary>
    public bool IsSingleSelect => MaxSelections <= 1;

    public ProductOption AddOption(string name, decimal priceDelta, int displayOrder = 0, string? banglaName = null,
        Guid? inventoryItemId = null, decimal consumeQtyBase = 0m)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Option name is required.", nameof(name));

        var option = new ProductOption
        {
            OptionGroupId = Id,
            Name = name.Trim(),
            BanglaName = string.IsNullOrWhiteSpace(banglaName) ? null : banglaName.Trim(),
            PriceDelta = priceDelta,
            DisplayOrder = displayOrder,
            IsActive = true,
            InventoryItemId = inventoryItemId,
            ConsumeQtyBase = inventoryItemId is null ? 0m : consumeQtyBase
        };
        _options.Add(option);
        return option;
    }

    /// <summary>Reconciles the option list with the desired state (update / add / remove). Requires Options loaded.</summary>
    public void SyncOptions(IReadOnlyList<(Guid? Id, string Name, string? BanglaName, decimal PriceDelta, int DisplayOrder, Guid? InventoryItemId, decimal ConsumeQtyBase)> desired)
    {
        var keepIds = new HashSet<Guid>();
        foreach (var row in desired)
        {
            if (string.IsNullOrWhiteSpace(row.Name)) throw new ArgumentException("Option name is required.");

            var existing = row.Id is { } id ? _options.FirstOrDefault(o => o.Id == id) : null;
            if (existing is not null)
            {
                existing.Name = row.Name.Trim();
                existing.BanglaName = string.IsNullOrWhiteSpace(row.BanglaName) ? null : row.BanglaName.Trim();
                existing.PriceDelta = row.PriceDelta;
                existing.DisplayOrder = row.DisplayOrder;
                existing.InventoryItemId = row.InventoryItemId;
                existing.ConsumeQtyBase = row.InventoryItemId is null ? 0m : row.ConsumeQtyBase;
                keepIds.Add(existing.Id);
            }
            else
            {
                keepIds.Add(AddOption(row.Name, row.PriceDelta, row.DisplayOrder, row.BanglaName, row.InventoryItemId, row.ConsumeQtyBase).Id);
            }
        }
        _options.RemoveAll(o => !keepIds.Contains(o.Id));
    }
}
