using BornoBit.Restaurant.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class MenuSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MenuSeeder> _logger;

    public MenuSeeder(ApplicationDbContext db, ILogger<MenuSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _db.MenuCategories.AnyAsync()) return;

        var mains = MenuCategory.Create("Main Course", displayOrder: 1, description: "Hearty mains");
        var drinks = MenuCategory.Create("Drinks", displayOrder: 2, description: "Refreshing beverages");

        _db.MenuCategories.Add(mains);
        _db.MenuCategories.Add(drinks);
        await _db.SaveChangesAsync();

        var items = new[]
        {
            MenuItem.Create(mains.Id, "MAIN-001", "Chicken Biryani", 280m, "Tk", "Aromatic basmati rice with spiced chicken", displayOrder: 1),
            MenuItem.Create(mains.Id, "MAIN-002", "Beef Tehari", 250m, "Tk", "Spiced beef rice", displayOrder: 2),
            MenuItem.Create(mains.Id, "MAIN-003", "Grilled Chicken", 320m, "Tk", "Char-grilled chicken with salad", displayOrder: 3),
            MenuItem.Create(drinks.Id, "DRK-001", "Borhani", 60m, "Tk", "Traditional spiced yogurt drink", displayOrder: 1),
            MenuItem.Create(drinks.Id, "DRK-002", "Lemon Mint", 80m, "Tk", "Fresh lemon and mint cooler", displayOrder: 2),
            MenuItem.Create(drinks.Id, "DRK-003", "Cola", 50m, "Tk", "Chilled soft drink", displayOrder: 3),
        };

        _db.MenuItems.AddRange(items);
        await _db.SaveChangesAsync();

        _logger.LogInformation("MenuSeeder: seeded {Categories} categories and {Items} items.", 2, items.Length);
    }
}
