using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Catalog;

/// <summary>
/// Bill of materials (BOM) for a <see cref="InventoryMethod.RecipeBased"/> product: the ingredients
/// consumed to produce <see cref="Yield"/> servings. When an order line is cooked, each
/// <see cref="RecipeItem"/> is converted to its base unit and deducted from the linked stock item.
/// An optional <see cref="VariantId"/> lets a specific variant (e.g. Full vs Half) carry its own BOM;
/// a recipe with <c>VariantId == null</c> is the fallback for the base product / all variants.
/// </summary>
public class Recipe : AuditableEntity
{
    public Guid ProductId { get; private set; }
    /// <summary>Variant this BOM applies to. Null = applies to the base product / any variant (fallback).</summary>
    public Guid? VariantId { get; private set; }
    /// <summary>Servings produced by one batch of this recipe. Ingredient qty is scaled by <c>line.Quantity / Yield</c>.</summary>
    public decimal Yield { get; private set; } = 1m;
    public bool IsActive { get; private set; } = true;

    private readonly List<RecipeItem> _items = new();
    public IReadOnlyCollection<RecipeItem> Items => _items.AsReadOnly();

    private Recipe() { }

    public static Recipe Create(Guid productId, Guid? variantId = null, decimal yield = 1m)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Product is required.", nameof(productId));
        if (yield <= 0m) throw new ArgumentOutOfRangeException(nameof(yield), "Yield must be positive.");

        return new Recipe
        {
            ProductId = productId,
            VariantId = variantId,
            Yield = yield,
            IsActive = true
        };
    }

    public RecipeItem AddItem(Guid inventoryItemId, decimal quantity, Guid unitId)
    {
        if (inventoryItemId == Guid.Empty) throw new ArgumentException("Ingredient is required.", nameof(inventoryItemId));
        if (quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (unitId == Guid.Empty) throw new ArgumentException("Unit is required.", nameof(unitId));

        var item = new RecipeItem
        {
            RecipeId = Id,
            InventoryItemId = inventoryItemId,
            Quantity = quantity,
            UnitId = unitId
        };
        _items.Add(item);
        return item;
    }

    /// <summary>
    /// Reconciles the item list with the desired state: rows with a matching Id are updated, rows
    /// without an Id are added, and existing items not present are removed. Requires <see cref="Items"/> loaded.
    /// </summary>
    public void SyncItems(IReadOnlyList<(Guid? Id, Guid InventoryItemId, decimal Quantity, Guid UnitId)> desired)
    {
        var keepIds = new HashSet<Guid>();

        foreach (var row in desired)
        {
            if (row.InventoryItemId == Guid.Empty) throw new ArgumentException("Ingredient is required.");
            if (row.Quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(desired), "Quantity must be positive.");
            if (row.UnitId == Guid.Empty) throw new ArgumentException("Unit is required.");

            var existing = row.Id is { } id ? _items.FirstOrDefault(i => i.Id == id) : null;
            if (existing is not null)
            {
                existing.InventoryItemId = row.InventoryItemId;
                existing.Quantity = row.Quantity;
                existing.UnitId = row.UnitId;
                keepIds.Add(existing.Id);
            }
            else
            {
                keepIds.Add(AddItem(row.InventoryItemId, row.Quantity, row.UnitId).Id);
            }
        }

        _items.RemoveAll(i => !keepIds.Contains(i.Id));
    }

    public void SetYield(decimal yield)
    {
        if (yield <= 0m) throw new ArgumentOutOfRangeException(nameof(yield), "Yield must be positive.");
        Yield = yield;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
