using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KitchenEntity = BornoBit.Restaurant.Domain.Kitchen.Kitchen;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the default physical kitchen and parents any unassigned stations to it. Idempotent.
/// Runs AFTER <see cref="KitchenStationSeeder"/> so the stations exist to be parented.
/// </summary>
public class KitchenSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<KitchenSeeder> _logger;

    public KitchenSeeder(ApplicationDbContext db, ILogger<KitchenSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // 1. Ensure a default kitchen exists (catch-all for station-less lines).
        var defaultKitchen = await _db.Kitchens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(k => k.IsDefault);

        if (defaultKitchen is null)
        {
            // Reuse an existing "Main Kitchen" by name if one was created without the default flag.
            defaultKitchen = await _db.Kitchens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.Name == "Main Kitchen");

            if (defaultKitchen is null)
            {
                defaultKitchen = KitchenEntity.Create("Main Kitchen", "MAIN", "#0F766E", printerName: null, displayOrder: 1, isDefault: true);
                _db.Kitchens.Add(defaultKitchen);
                _logger.LogInformation("KitchenSeeder: seeded default kitchen 'Main Kitchen'.");
            }
            else
            {
                defaultKitchen.SetDefault(true);
            }
            await _db.SaveChangesAsync();
        }

        // 2. Parent any station that has no kitchen yet to the default kitchen.
        var orphanStations = await _db.KitchenStations
            .Where(s => s.KitchenId == null)
            .ToListAsync();

        if (orphanStations.Count > 0)
        {
            foreach (var station in orphanStations)
                station.AssignKitchen(defaultKitchen.Id);
            await _db.SaveChangesAsync();
            _logger.LogInformation("KitchenSeeder: parented {Count} station(s) to '{Kitchen}'.", orphanStations.Count, defaultKitchen.Name);
        }
    }
}
