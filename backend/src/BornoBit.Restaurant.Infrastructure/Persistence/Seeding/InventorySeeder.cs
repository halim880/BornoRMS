using BornoBit.Restaurant.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class InventorySeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<InventorySeeder> _logger;

    public InventorySeeder(ApplicationDbContext db, ILogger<InventorySeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedCatalogAsync();
        await SeedVariantsAsync();
    }

    private async Task SeedCatalogAsync()
    {
        if (await _db.ProductCategories.AnyAsync()) return;

        var biryani = ProductCategory.Create("Biryani & Rice", displayOrder: 1, description: "বিরিয়ানি ও ভাত — rice-based mains");
        var kabab = ProductCategory.Create("Kabab & Grill", displayOrder: 2, description: "কাবাব ও গ্রিল — grilled & skewered");
        var curry = ProductCategory.Create("Curry / Bhuna", displayOrder: 3, description: "তরকারি — traditional curries");
        var bhorta = ProductCategory.Create("Bhorta & Bhaji", displayOrder: 4, description: "ভর্তা ও ভাজি — mashes & fries");
        var bread = ProductCategory.Create("Bread", displayOrder: 5, description: "রুটি ও নান — breads");
        var mishti = ProductCategory.Create("Mishti / Dessert", displayOrder: 6, description: "মিষ্টি — sweets");
        var beverage = ProductCategory.Create("Beverage", displayOrder: 7, description: "পানীয় — drinks");

        var categories = new[] { biryani, kabab, curry, bhorta, bread, mishti, beverage };
        _db.ProductCategories.AddRange(categories);
        await _db.SaveChangesAsync();

        var products = new[]
        {
            // Biryani & Rice
            Product.Create(biryani.Id, "BIR-001", "Kacchi Biryani", 320m, "কাচ্চি বিরিয়ানি", description: "Mutton kacchi with aromatic rice", displayOrder: 1),
            Product.Create(biryani.Id, "BIR-002", "Chicken Biryani", 250m, "চিকেন বিরিয়ানি", description: "Spiced chicken biryani", displayOrder: 2),
            Product.Create(biryani.Id, "BIR-003", "Morog Polao", 280m, "মোরগ পোলাও", description: "Fragrant chicken pulao", displayOrder: 3),
            Product.Create(biryani.Id, "BIR-004", "Plain Polao", 120m, "সাদা পোলাও", description: "Buttered white pulao", displayOrder: 4),

            // Kabab & Grill
            Product.Create(kabab.Id, "KAB-001", "Chicken Tikka", 180m, "চিকেন টিক্কা", description: "Marinated grilled chicken", displayOrder: 1),
            Product.Create(kabab.Id, "KAB-002", "Beef Seekh Kabab", 160m, "বিফ শিক কাবাব", description: "Minced beef skewers", displayOrder: 2),
            Product.Create(kabab.Id, "KAB-003", "Jali Kabab", 90m, "জালি কাবাব", description: "Net-wrapped beef kabab", displayOrder: 3),
            Product.Create(kabab.Id, "KAB-004", "Chicken Grill (Half)", 220m, "চিকেন গ্রিল", description: "Half char-grilled chicken", displayOrder: 4),

            // Curry / Bhuna
            Product.Create(curry.Id, "CUR-001", "Beef Bhuna", 240m, "গরুর মাংস ভুনা", description: "Slow-cooked spiced beef", displayOrder: 1),
            Product.Create(curry.Id, "CUR-002", "Chicken Curry", 200m, "মুরগির ঝোল", description: "Home-style chicken curry", displayOrder: 2),
            Product.Create(curry.Id, "CUR-003", "Rui Fish Curry", 220m, "রুই মাছের তরকারি", description: "Rohu fish in light gravy", displayOrder: 3),
            Product.Create(curry.Id, "CUR-004", "Daal", 70m, "ডাল", description: "Yellow lentil daal", displayOrder: 4),

            // Bhorta & Bhaji
            Product.Create(bhorta.Id, "BHR-001", "Aloo Bhorta", 60m, "আলু ভর্তা", description: "Mashed potato with mustard oil", displayOrder: 1),
            Product.Create(bhorta.Id, "BHR-002", "Begun Bhorta", 70m, "বেগুন ভর্তা", description: "Smoked eggplant mash", displayOrder: 2),
            Product.Create(bhorta.Id, "BHR-003", "Shutki Bhorta", 120m, "শুঁটকি ভর্তা", description: "Dried fish mash", displayOrder: 3),
            Product.Create(bhorta.Id, "BHR-004", "Dal Bhaji", 60m, "ডাল ভাজি", description: "Stir-fried lentils", displayOrder: 4),

            // Bread
            Product.Create(bread.Id, "BRD-001", "Naan", 30m, "নান রুটি", description: "Tandoor naan", displayOrder: 1),
            Product.Create(bread.Id, "BRD-002", "Porota", 20m, "পরোটা", description: "Layered fried flatbread", displayOrder: 2),
            Product.Create(bread.Id, "BRD-003", "Tandoori Roti", 25m, "তান্দুরি রুটি", description: "Whole-wheat tandoor roti", displayOrder: 3),

            // Mishti / Dessert
            Product.Create(mishti.Id, "MIS-001", "Roshogolla", 40m, "রসগোল্লা", description: "Spongy syrup sweet", displayOrder: 1),
            Product.Create(mishti.Id, "MIS-002", "Mishti Doi", 80m, "মিষ্টি দই", description: "Sweet caramelised yogurt", displayOrder: 2),
            Product.Create(mishti.Id, "MIS-003", "Firni", 90m, "ফিরনি", description: "Ground-rice pudding", displayOrder: 3),

            // Beverage
            Product.Create(beverage.Id, "BEV-001", "Borhani", 60m, "বোরহানি", description: "Spiced yogurt drink", displayOrder: 1),
            Product.Create(beverage.Id, "BEV-002", "Lassi", 90m, "লাচ্ছি", description: "Sweet yogurt smoothie", displayOrder: 2),
            Product.Create(beverage.Id, "BEV-003", "Lemon Mint", 80m, "লেমন মিন্ট", description: "Fresh lemon-mint cooler", displayOrder: 3),
            Product.Create(beverage.Id, "BEV-004", "Cha", 20m, "চা", description: "Milk tea", displayOrder: 4),
        };

        _db.Products.AddRange(products);
        await _db.SaveChangesAsync();

        _logger.LogInformation("InventorySeeder: seeded {Categories} categories and {Products} products.",
            categories.Length, products.Length);
    }

    /// <summary>
    /// Attaches size/portion variants to a few known products. Idempotent (skips when any
    /// variants exist) and safe on databases seeded before variants existed.
    /// </summary>
    private async Task SeedVariantsAsync()
    {
        if (await _db.ProductVariants.AnyAsync()) return;

        var byCode = await _db.Products
            .Include(p => p.Variants)
            .Where(p => p.Code == "KAB-004" || p.Code == "BEV-001" || p.Code == "BEV-002" || p.Code == "BEV-003")
            .ToDictionaryAsync(p => p.Code);

        if (byCode.Count == 0) return;

        var created = new List<ProductVariant>();

        if (byCode.TryGetValue("KAB-004", out var grill))
        {
            // Older seed named this "Chicken Grill (Half)" — the portion now lives on the variant.
            if (grill.Name.Contains("(Half)"))
                grill.UpdateDetails(grill.ProductCategoryId, grill.Code, "Chicken Grill", grill.Price,
                    grill.BanglaName, grill.ImagePath, grill.Description, grill.DisplayOrder);

            created.Add(grill.AddVariant("Quarter", 120m, 1));
            created.Add(grill.AddVariant("Half", 220m, 2));
            created.Add(grill.AddVariant("Full", 420m, 3));
        }

        if (byCode.TryGetValue("BEV-001", out var borhani))
        {
            created.Add(borhani.AddVariant("Glass", 60m, 1));
            created.Add(borhani.AddVariant("Jug", 220m, 2));
        }

        if (byCode.TryGetValue("BEV-002", out var lassi))
        {
            created.Add(lassi.AddVariant("Regular", 90m, 1));
            created.Add(lassi.AddVariant("Large", 130m, 2));
        }

        if (byCode.TryGetValue("BEV-003", out var lemonMint))
        {
            created.Add(lemonMint.AddVariant("Regular", 80m, 1));
            created.Add(lemonMint.AddVariant("Large", 120m, 2));
        }

        // New children hanging off a tracked parent are discovered with their pre-set Guid key and
        // would be saved as UPDATEs (0 rows → concurrency exception) — mark them Added explicitly.
        _db.ProductVariants.AddRange(created);

        var added = await _db.SaveChangesAsync();
        _logger.LogInformation("InventorySeeder: seeded product variants ({Rows} rows).", added);
    }
}
