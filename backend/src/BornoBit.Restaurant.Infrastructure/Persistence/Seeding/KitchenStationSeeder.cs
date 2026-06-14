using BornoBit.Restaurant.Domain.Kitchen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class KitchenStationSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<KitchenStationSeeder> _logger;

    public KitchenStationSeeder(ApplicationDbContext db, ILogger<KitchenStationSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static readonly (string Name, string Code, string Color, int Order)[] Defaults =
    {
        ("Grill",    "GRILL", "#DC2626", 1),
        ("Rice / Curry", "RICE", "#D97706", 2),
        ("Fry / Snacks", "FRY", "#CA8A04", 3),
        ("Beverage", "BEV",   "#0E7490", 4),
        ("Dessert",  "DESS",  "#7C3AED", 5),
    };

    public async Task SeedAsync()
    {
        var existing = await _db.KitchenStations
            .IgnoreQueryFilters()
            .Select(s => s.Name)
            .ToListAsync();
        var have = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var (name, code, color, order) in Defaults)
        {
            if (have.Contains(name)) continue;
            _db.KitchenStations.Add(KitchenStation.Create(name, code, color, order));
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("KitchenStationSeeder: seeded {Count} stations.", added);
        }
    }
}
