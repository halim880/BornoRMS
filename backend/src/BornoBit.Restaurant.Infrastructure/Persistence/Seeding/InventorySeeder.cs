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

    /// <summary>Product Code → bundled image path under wwwroot. Used by both fresh seed and backfill.</summary>
    private static readonly Dictionary<string, string> ImageByCode = new()
    {
        ["BIR-001"] = "/img/products/kacchi-biryani.jpg",
        ["BIR-002"] = "/img/products/chicken-biryani.jpg",
        ["BIR-003"] = "/img/products/morog-polao.jpg",
        ["BIR-004"] = "/img/products/plain-polao.jpg",
        ["KAB-001"] = "/img/products/chicken-tikka.jpg",
        ["KAB-002"] = "/img/products/seekh-kabab.jpg",
        ["KAB-003"] = "/img/products/jali-kabab.jpg",
        ["KAB-004"] = "/img/products/chicken-grill.jpg",
        ["CUR-001"] = "/img/products/beef-bhuna.jpg",
        ["CUR-002"] = "/img/products/chicken-curry.jpg",
        ["CUR-003"] = "/img/products/fish-curry.jpg",
        ["CUR-004"] = "/img/products/daal.jpg",
        ["BHR-001"] = "/img/products/aloo-bhorta.jpg",
        ["BHR-002"] = "/img/products/begun-bhorta.jpg",
        ["BHR-003"] = "/img/products/shutki-bhorta.jpg",
        ["BHR-004"] = "/img/products/dal-bhaji.jpg",
        ["BRD-001"] = "/img/products/naan.jpg",
        ["BRD-002"] = "/img/products/porota.jpg",
        ["BRD-003"] = "/img/products/tandoori-roti.jpg",
        ["MIS-001"] = "/img/products/roshogolla.jpg",
        ["MIS-002"] = "/img/products/mishti-doi.jpg",
        ["MIS-003"] = "/img/products/firni.jpg",
        ["BEV-001"] = "/img/products/borhani.jpg",
        ["BEV-002"] = "/img/products/lassi.jpg",
        ["BEV-003"] = "/img/products/lemon-mint.jpg",
        ["BEV-004"] = "/img/products/cha.jpg",
    };

    public async Task SeedAsync()
    {
        await SeedCatalogAsync();
        await BackfillImagesAsync();
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
            Product.Create(biryani.Id, "BIR-001", "Kacchi Biryani", 320m, "কাচ্চি বিরিয়ানি", imagePath: ImageByCode["BIR-001"], description: "Mutton kacchi with aromatic rice", displayOrder: 1),
            Product.Create(biryani.Id, "BIR-002", "Chicken Biryani", 250m, "চিকেন বিরিয়ানি", imagePath: ImageByCode["BIR-002"], description: "Spiced chicken biryani", displayOrder: 2),
            Product.Create(biryani.Id, "BIR-003", "Morog Polao", 280m, "মোরগ পোলাও", imagePath: ImageByCode["BIR-003"], description: "Fragrant chicken pulao", displayOrder: 3),
            Product.Create(biryani.Id, "BIR-004", "Plain Polao", 120m, "সাদা পোলাও", imagePath: ImageByCode["BIR-004"], description: "Buttered white pulao", displayOrder: 4),

            // Kabab & Grill
            Product.Create(kabab.Id, "KAB-001", "Chicken Tikka", 180m, "চিকেন টিক্কা", imagePath: ImageByCode["KAB-001"], description: "Marinated grilled chicken", displayOrder: 1),
            Product.Create(kabab.Id, "KAB-002", "Beef Seekh Kabab", 160m, "বিফ শিক কাবাব", imagePath: ImageByCode["KAB-002"], description: "Minced beef skewers", displayOrder: 2),
            Product.Create(kabab.Id, "KAB-003", "Jali Kabab", 90m, "জালি কাবাব", imagePath: ImageByCode["KAB-003"], description: "Net-wrapped beef kabab", displayOrder: 3),
            Product.Create(kabab.Id, "KAB-004", "Chicken Grill (Half)", 220m, "চিকেন গ্রিল", imagePath: ImageByCode["KAB-004"], description: "Half char-grilled chicken", displayOrder: 4),

            // Curry / Bhuna
            Product.Create(curry.Id, "CUR-001", "Beef Bhuna", 240m, "গরুর মাংস ভুনা", imagePath: ImageByCode["CUR-001"], description: "Slow-cooked spiced beef", displayOrder: 1),
            Product.Create(curry.Id, "CUR-002", "Chicken Curry", 200m, "মুরগির ঝোল", imagePath: ImageByCode["CUR-002"], description: "Home-style chicken curry", displayOrder: 2),
            Product.Create(curry.Id, "CUR-003", "Rui Fish Curry", 220m, "রুই মাছের তরকারি", imagePath: ImageByCode["CUR-003"], description: "Rohu fish in light gravy", displayOrder: 3),
            Product.Create(curry.Id, "CUR-004", "Daal", 70m, "ডাল", imagePath: ImageByCode["CUR-004"], description: "Yellow lentil daal", displayOrder: 4),

            // Bhorta & Bhaji
            Product.Create(bhorta.Id, "BHR-001", "Aloo Bhorta", 60m, "আলু ভর্তা", imagePath: ImageByCode["BHR-001"], description: "Mashed potato with mustard oil", displayOrder: 1),
            Product.Create(bhorta.Id, "BHR-002", "Begun Bhorta", 70m, "বেগুন ভর্তা", imagePath: ImageByCode["BHR-002"], description: "Smoked eggplant mash", displayOrder: 2),
            Product.Create(bhorta.Id, "BHR-003", "Shutki Bhorta", 120m, "শুঁটকি ভর্তা", imagePath: ImageByCode["BHR-003"], description: "Dried fish mash", displayOrder: 3),
            Product.Create(bhorta.Id, "BHR-004", "Dal Bhaji", 60m, "ডাল ভাজি", imagePath: ImageByCode["BHR-004"], description: "Stir-fried lentils", displayOrder: 4),

            // Bread
            Product.Create(bread.Id, "BRD-001", "Naan", 30m, "নান রুটি", imagePath: ImageByCode["BRD-001"], description: "Tandoor naan", displayOrder: 1),
            Product.Create(bread.Id, "BRD-002", "Porota", 20m, "পরোটা", imagePath: ImageByCode["BRD-002"], description: "Layered fried flatbread", displayOrder: 2),
            Product.Create(bread.Id, "BRD-003", "Tandoori Roti", 25m, "তান্দুরি রুটি", imagePath: ImageByCode["BRD-003"], description: "Whole-wheat tandoor roti", displayOrder: 3),

            // Mishti / Dessert
            Product.Create(mishti.Id, "MIS-001", "Roshogolla", 40m, "রসগোল্লা", imagePath: ImageByCode["MIS-001"], description: "Spongy syrup sweet", displayOrder: 1),
            Product.Create(mishti.Id, "MIS-002", "Mishti Doi", 80m, "মিষ্টি দই", imagePath: ImageByCode["MIS-002"], description: "Sweet caramelised yogurt", displayOrder: 2),
            Product.Create(mishti.Id, "MIS-003", "Firni", 90m, "ফিরনি", imagePath: ImageByCode["MIS-003"], description: "Ground-rice pudding", displayOrder: 3),

            // Beverage
            Product.Create(beverage.Id, "BEV-001", "Borhani", 60m, "বোরহানি", imagePath: ImageByCode["BEV-001"], description: "Spiced yogurt drink", displayOrder: 1),
            Product.Create(beverage.Id, "BEV-002", "Lassi", 90m, "লাচ্ছি", imagePath: ImageByCode["BEV-002"], description: "Sweet yogurt smoothie", displayOrder: 2),
            Product.Create(beverage.Id, "BEV-003", "Lemon Mint", 80m, "লেমন মিন্ট", imagePath: ImageByCode["BEV-003"], description: "Fresh lemon-mint cooler", displayOrder: 3),
            Product.Create(beverage.Id, "BEV-004", "Cha", 20m, "চা", imagePath: ImageByCode["BEV-004"], description: "Milk tea", displayOrder: 4),
        };

        _db.Products.AddRange(products);
        await _db.SaveChangesAsync();

        _logger.LogInformation("InventorySeeder: seeded {Categories} categories and {Products} products.",
            categories.Length, products.Length);
    }

    /// <summary>
    /// Backfills bundled image paths onto products seeded before images existed. Idempotent:
    /// only touches rows whose Code is in <see cref="ImageByCode"/> and whose ImagePath is still
    /// null, so it is a no-op once images are set. This is what updates an already-seeded DB.
    /// </summary>
    private async Task BackfillImagesAsync()
    {
        var codes = ImageByCode.Keys.ToList();
        var targets = await _db.Products
            .Where(p => codes.Contains(p.Code) && p.ImagePath == null)
            .ToListAsync();

        if (targets.Count == 0) return;

        foreach (var p in targets)
            p.UpdateDetails(p.ProductCategoryId, p.Code, p.Name, p.Price,
                p.BanglaName, ImageByCode[p.Code], p.Description, p.DisplayOrder);

        var rows = await _db.SaveChangesAsync();
        _logger.LogInformation("InventorySeeder: backfilled images on {Count} products ({Rows} rows).",
            targets.Count, rows);
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
