using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Consumption;

/// <summary>One stock requirement produced by exploding order lines: how much of an item to deduct, in base units.</summary>
public record StockRequirement(Guid InventoryItemId, decimal QtyBase);

/// <summary>
/// Shared resolver that turns an order's lines into the stock each one consumes, used by BOTH the
/// consumption engine and the pre-confirm availability check so the two never diverge.
/// <list type="bullet">
/// <item><see cref="InventoryMethod.RecipeBased"/> — explode the product's <c>Recipe</c>; each ingredient
/// is converted to base units via <c>Unit.ToBase</c> and scaled by <c>line.Quantity / Recipe.Yield</c>.</item>
/// <item><see cref="InventoryMethod.DirectStock"/> — deduct the linked <c>InventoryItem</c> (by ProductId)
/// by <c>line.Quantity</c> base units.</item>
/// <item><see cref="InventoryMethod.None"/> — skipped.</item>
/// </list>
/// Requirements are aggregated per item (a shared ingredient across lines sums into one row).
/// </summary>
public static class RecipeExploder
{
    /// <summary>An order line reduced to the fields consumption needs.</summary>
    public record LineInput(Guid ProductId, Guid? VariantId, int Quantity);

    public static async Task<IReadOnlyList<StockRequirement>> ExplodeAsync(
        IAppDbContext db, IReadOnlyCollection<LineInput> lines, CancellationToken ct)
    {
        if (lines.Count == 0) return Array.Empty<StockRequirement>();

        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();

        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.InventoryMethod })
            .ToDictionaryAsync(p => p.Id, p => p.InventoryMethod, ct);

        var recipeProductIds = products.Where(p => p.Value == InventoryMethod.RecipeBased).Select(p => p.Key).ToList();
        var directProductIds = products.Where(p => p.Value == InventoryMethod.DirectStock).Select(p => p.Key).ToList();

        // Recipes (+ items) for recipe-based products.
        var recipes = recipeProductIds.Count == 0
            ? new List<Recipe>()
            : await db.Recipes
                .Include(r => r.Items)
                .Where(r => recipeProductIds.Contains(r.ProductId) && r.IsActive)
                .ToListAsync(ct);

        // Unit conversion factors for every recipe-item unit.
        var unitIds = recipes.SelectMany(r => r.Items).Select(i => i.UnitId).Distinct().ToList();
        var unitFactors = unitIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await db.Units.Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.ToBaseFactor, ct);

        // Direct-stock product → linked inventory item, keyed per (product, variant). A non-variant product
        // links one item with VariantId == null; a variant product links one item per variant.
        var directLinks = directProductIds.Count == 0
            ? new Dictionary<(Guid ProductId, Guid? VariantId), Guid>()
            : await db.InventoryItems
                .Where(i => i.ProductId != null && directProductIds.Contains(i.ProductId!.Value))
                .Select(i => new { ProductId = i.ProductId!.Value, i.VariantId, i.Id })
                .ToDictionaryAsync(x => (x.ProductId, x.VariantId), x => x.Id, ct);

        var required = new Dictionary<Guid, decimal>();

        void Add(Guid itemId, decimal qtyBase)
        {
            if (qtyBase <= 0m) return;
            required[itemId] = required.TryGetValue(itemId, out var cur) ? cur + qtyBase : qtyBase;
        }

        foreach (var line in lines)
        {
            if (!products.TryGetValue(line.ProductId, out var method)) continue;

            switch (method)
            {
                case InventoryMethod.DirectStock:
                    // Variant-specific link wins; fall back to the product-level link (VariantId == null).
                    if (directLinks.TryGetValue((line.ProductId, line.VariantId), out var directItemId)
                        || directLinks.TryGetValue((line.ProductId, (Guid?)null), out directItemId))
                        Add(directItemId, line.Quantity);
                    break;

                case InventoryMethod.RecipeBased:
                    var recipe = recipes.FirstOrDefault(r => r.ProductId == line.ProductId && r.VariantId == line.VariantId)
                                 ?? recipes.FirstOrDefault(r => r.ProductId == line.ProductId && r.VariantId == null);
                    if (recipe is null) continue;
                    foreach (var ri in recipe.Items)
                    {
                        var factor = unitFactors.TryGetValue(ri.UnitId, out var f) ? f : 1m;
                        var qtyBase = ri.Quantity * factor * line.Quantity / recipe.Yield;
                        Add(ri.InventoryItemId, qtyBase);
                    }
                    break;

                // InventoryMethod.None → no stock impact.
            }
        }

        return required.Select(kv => new StockRequirement(kv.Key, kv.Value)).ToList();
    }
}
