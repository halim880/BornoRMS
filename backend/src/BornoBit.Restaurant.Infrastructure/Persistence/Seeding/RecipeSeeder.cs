using BornoBit.Restaurant.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds demo recipes (BOM) for a few menu products and flips them to <see cref="InventoryMethod.RecipeBased"/>.
/// Quantities are in grams / millilitres to exercise unit conversion (GM→KG, ML→LTR) at consume time.
/// Idempotent and defensive: skips if recipes already exist or the referenced products/ingredients are absent.
/// Depends on <see cref="InventorySeeder"/> (products), <see cref="StockSeeder"/> (ingredients) and
/// <see cref="UnitSeeder"/> (units).
/// </summary>
public class RecipeSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RecipeSeeder> _logger;

    public RecipeSeeder(ApplicationDbContext db, ILogger<RecipeSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    // (ProductCode, Yield, [(IngredientCode, Qty, UnitCode)])
    private static readonly (string Product, decimal Yield, (string Item, decimal Qty, string Unit)[] Items)[] Recipes =
    {
        ("BIR-002", 1m, new[]
        {
            ("ING-CHINIGURA", 250m, "GM"), ("ING-CHICKEN", 300m, "GM"), ("ING-PIYAJ", 100m, "GM"),
            ("ING-TEL", 50m, "ML"), ("ING-HOLUD", 5m, "GM"), ("ING-GHEE", 20m, "GM"),
        }),
        ("CUR-002", 1m, new[]
        {
            ("ING-CHICKEN", 250m, "GM"), ("ING-PIYAJ", 80m, "GM"), ("ING-TEL", 30m, "ML"),
            ("ING-MORICH", 5m, "GM"), ("ING-HOLUD", 4m, "GM"),
        }),
        ("CUR-004", 1m, new[]
        {
            ("ING-MOSHURDAL", 80m, "GM"), ("ING-PIYAJ", 20m, "GM"), ("ING-TEL", 10m, "ML"), ("ING-HOLUD", 2m, "GM"),
        }),
    };

    public async Task SeedAsync()
    {
        if (await _db.Recipes.AnyAsync()) return;

        var products = await _db.Products.ToDictionaryAsync(p => p.Code, p => p);
        var items = await _db.InventoryItems.ToDictionaryAsync(i => i.Code, i => i.Id);
        var units = await _db.Units.ToDictionaryAsync(u => u.Code, u => u.Id);

        var seeded = 0;
        foreach (var def in Recipes)
        {
            if (!products.TryGetValue(def.Product, out var product))
            {
                _logger.LogWarning("RecipeSeeder: product {Code} not found; skipping recipe.", def.Product);
                continue;
            }

            var resolved = def.Items
                .Where(x => items.ContainsKey(x.Item) && units.ContainsKey(x.Unit))
                .Select(x => (items[x.Item], x.Qty, units[x.Unit]))
                .ToList();
            if (resolved.Count == 0) continue;

            var recipe = Recipe.Create(product.Id, variantId: null, yield: def.Yield);
            foreach (var (itemId, qty, unitId) in resolved)
                recipe.AddItem(itemId, qty, unitId);

            _db.Recipes.Add(recipe);
            product.SetInventoryMethod(InventoryMethod.RecipeBased);
            seeded++;
        }

        if (seeded > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("RecipeSeeder: seeded {Count} recipes.", seeded);
        }
    }
}
