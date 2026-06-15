using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Consumption;

/// <summary>
/// Flags order lines that are meant to move stock on sale but have nothing configured to deduct — a
/// DirectStock product/variant with no linked SKU, or a RecipeBased one with no active recipe. Used to
/// surface a non-blocking warning to the cashier (the sale still completes; stock just won't move).
/// </summary>
public static class StockTrackingInspector
{
    public static async Task<IReadOnlyList<string>> FindUntrackedAsync(IAppDbContext db, Order order, CancellationToken ct)
    {
        if (order.Lines.Count == 0) return Array.Empty<string>();

        var productIds = order.Lines.Select(l => l.MenuItemId).Distinct().ToList();

        var methods = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.InventoryMethod })
            .ToDictionaryAsync(p => p.Id, p => p.InventoryMethod, ct);

        var directSet = (await db.InventoryItems
            .Where(i => i.ProductId != null && productIds.Contains(i.ProductId!.Value))
            .Select(i => new { ProductId = i.ProductId!.Value, i.VariantId })
            .ToListAsync(ct))
            .Select(x => (x.ProductId, x.VariantId)).ToHashSet();

        var recipeSet = (await db.Recipes
            .Where(r => productIds.Contains(r.ProductId) && r.IsActive)
            .Select(r => new { r.ProductId, r.VariantId })
            .ToListAsync(ct))
            .Select(x => (x.ProductId, x.VariantId)).ToHashSet();

        var warnings = new List<string>();
        var seen = new HashSet<(Guid, Guid?)>();
        foreach (var line in order.Lines)
        {
            if (!seen.Add((line.MenuItemId, line.VariantId))) continue;
            if (!methods.TryGetValue(line.MenuItemId, out var method)) continue;

            switch (method)
            {
                case InventoryMethod.DirectStock
                    when !directSet.Contains((line.MenuItemId, line.VariantId)) && !directSet.Contains((line.MenuItemId, null)):
                    warnings.Add($"No SKU for {line.Name} — stock not deducted.");
                    break;
                case InventoryMethod.RecipeBased
                    when !recipeSet.Contains((line.MenuItemId, line.VariantId)) && !recipeSet.Contains((line.MenuItemId, null)):
                    warnings.Add($"No recipe for {line.Name} — stock not deducted.");
                    break;
            }
        }
        return warnings;
    }
}
