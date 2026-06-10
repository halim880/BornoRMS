using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds stock categories, suppliers and realistic Bangladeshi ingredients + finished goods.
/// Depends on <see cref="UnitSeeder"/> having run (looks up base units by code).
/// </summary>
public class StockSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<StockSeeder> _logger;

    public StockSeeder(ApplicationDbContext db, ILogger<StockSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _db.InventoryCategories.AnyAsync()) return;

        var units = await _db.Units.ToDictionaryAsync(u => u.Code, u => u.Id);
        if (units.Count == 0)
        {
            _logger.LogWarning("StockSeeder: no units seeded yet; skipping.");
            return;
        }

        Guid KG = units["KG"], LTR = units["LTR"], PCS = units["PCS"];

        // Categories
        var rice = InventoryCategory.Create("Rice & Lentils", 1, "চাল ও ডাল");
        var meat = InventoryCategory.Create("Meat", 2, "মাংস");
        var fish = InventoryCategory.Create("Fish", 3, "মাছ");
        var veg = InventoryCategory.Create("Vegetables", 4, "সবজি");
        var spice = InventoryCategory.Create("Spices", 5, "মশলা");
        var oil = InventoryCategory.Create("Oil & Ghee", 6, "তেল ও ঘি");
        var dairy = InventoryCategory.Create("Dairy & Egg", 7, "দুধ ও ডিম");
        var finished = InventoryCategory.Create("Packaged / Finished", 8, "প্যাকেটজাত");

        var categories = new[] { rice, meat, fish, veg, spice, oil, dairy, finished };
        _db.InventoryCategories.AddRange(categories);
        await _db.SaveChangesAsync();

        // Suppliers
        var bazar = Supplier.Create("SUP-BAZAR", "Karwan Bazar Vendor", "01710000000", "Karwan Bazar, Dhaka", 0, "Daily cash kacha bazar");
        var wholesale = Supplier.Create("SUP-WHOLE", "Moulvibazar Wholesale", "01810000000", "Moulvibazar, Dhaka", 7, "Dry goods & spices, 7-day terms");
        _db.Suppliers.AddRange(bazar, wholesale);
        await _db.SaveChangesAsync();

        // Items (QtyOnHand starts at 0 — receive via GRN). Reorder levels in base units.
        var items = new[]
        {
            InventoryItem.Create("ING-CHINIGURA", "Chinigura Rice", rice.Id, InventoryItemType.Ingredient, KG, "চিনিগুঁড়া চাল", reorderLevel: 25m, reorderQty: 75m, packSize: 50m, packNote: "1 bosta = 50 kg"),
            InventoryItem.Create("ING-MISWAR", "Miniket Rice", rice.Id, InventoryItemType.Ingredient, KG, "মিনিকেট চাল", reorderLevel: 25m, reorderQty: 75m, packSize: 50m, packNote: "1 bosta = 50 kg"),
            InventoryItem.Create("ING-MOSHURDAL", "Mushur Dal", rice.Id, InventoryItemType.Ingredient, KG, "মসুর ডাল", reorderLevel: 10m, reorderQty: 30m),

            InventoryItem.Create("ING-BEEF", "Beef", meat.Id, InventoryItemType.Ingredient, KG, "গরুর মাংস", reorderLevel: 10m, reorderQty: 40m, isPerishable: true),
            InventoryItem.Create("ING-MUTTON", "Mutton", meat.Id, InventoryItemType.Ingredient, KG, "খাসির মাংস", reorderLevel: 5m, reorderQty: 20m, isPerishable: true),
            InventoryItem.Create("ING-CHICKEN", "Chicken", meat.Id, InventoryItemType.Ingredient, KG, "মুরগি", reorderLevel: 10m, reorderQty: 40m, isPerishable: true),

            InventoryItem.Create("ING-RUI", "Rui Fish", fish.Id, InventoryItemType.Ingredient, KG, "রুই মাছ", reorderLevel: 5m, reorderQty: 20m, isPerishable: true),
            InventoryItem.Create("ING-ILISH", "Ilish Fish", fish.Id, InventoryItemType.Ingredient, KG, "ইলিশ মাছ", reorderLevel: 3m, reorderQty: 10m, isPerishable: true),

            InventoryItem.Create("ING-PIYAJ", "Onion", veg.Id, InventoryItemType.Ingredient, KG, "পিঁয়াজ", reorderLevel: 15m, reorderQty: 50m, isPerishable: true),
            InventoryItem.Create("ING-ROSHUN", "Garlic", veg.Id, InventoryItemType.Ingredient, KG, "রসুন", reorderLevel: 5m, reorderQty: 15m, isPerishable: true),
            InventoryItem.Create("ING-ADA", "Ginger", veg.Id, InventoryItemType.Ingredient, KG, "আদা", reorderLevel: 4m, reorderQty: 12m, isPerishable: true),
            InventoryItem.Create("ING-ALU", "Potato", veg.Id, InventoryItemType.Ingredient, KG, "আলু", reorderLevel: 15m, reorderQty: 50m, isPerishable: true),

            InventoryItem.Create("ING-HOLUD", "Turmeric Powder", spice.Id, InventoryItemType.Ingredient, KG, "হলুদ গুঁড়া", reorderLevel: 2m, reorderQty: 5m),
            InventoryItem.Create("ING-MORICH", "Chilli Powder", spice.Id, InventoryItemType.Ingredient, KG, "মরিচ গুঁড়া", reorderLevel: 2m, reorderQty: 5m),
            InventoryItem.Create("ING-JIRA", "Cumin", spice.Id, InventoryItemType.Ingredient, KG, "জিরা", reorderLevel: 1m, reorderQty: 3m),
            InventoryItem.Create("ING-ELACH", "Cardamom", spice.Id, InventoryItemType.Ingredient, KG, "এলাচ", reorderLevel: 0.5m, reorderQty: 2m),

            InventoryItem.Create("ING-TEL", "Soybean Oil", oil.Id, InventoryItemType.Ingredient, LTR, "সয়াবিন তেল", reorderLevel: 10m, reorderQty: 40m),
            InventoryItem.Create("ING-GHEE", "Ghee", oil.Id, InventoryItemType.Ingredient, KG, "ঘি", reorderLevel: 2m, reorderQty: 8m),

            InventoryItem.Create("ING-DUDH", "Milk", dairy.Id, InventoryItemType.Ingredient, LTR, "দুধ", reorderLevel: 5m, reorderQty: 20m, isPerishable: true),
            InventoryItem.Create("ING-DOI", "Yogurt (Doi)", dairy.Id, InventoryItemType.Ingredient, KG, "দই", reorderLevel: 3m, reorderQty: 10m, isPerishable: true),
            InventoryItem.Create("ING-DIM", "Egg", dairy.Id, InventoryItemType.Ingredient, PCS, "ডিম", reorderLevel: 60m, reorderQty: 240m, isPerishable: true),

            InventoryItem.Create("FG-WATER", "Bottled Water 500ml", finished.Id, InventoryItemType.FinishedGood, PCS, "পানির বোতল", reorderLevel: 48m, reorderQty: 144m),
            InventoryItem.Create("FG-COLA", "Cola Can 250ml", finished.Id, InventoryItemType.FinishedGood, PCS, "কোলা ক্যান", reorderLevel: 48m, reorderQty: 144m),
        };

        _db.InventoryItems.AddRange(items);
        await _db.SaveChangesAsync();

        _logger.LogInformation("StockSeeder: seeded {Categories} categories, {Suppliers} suppliers, {Items} items.",
            categories.Length, 2, items.Length);
    }
}
