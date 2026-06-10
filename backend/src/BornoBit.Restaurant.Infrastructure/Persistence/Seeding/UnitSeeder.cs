using BornoBit.Restaurant.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds units of measure including Bangladeshi traditional units. Base units (factor 1):
/// KG (weight), LTR (volume), PCS (count). Conversions: 1 seer = 0.9331 kg, 1 mon = 40 seer = 37.3242 kg,
/// 1 hali = 4 pcs, 1 dozen = 12 pcs.
/// </summary>
public class UnitSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UnitSeeder> _logger;

    public UnitSeeder(ApplicationDbContext db, ILogger<UnitSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _db.Units.AnyAsync()) return;

        var units = new[]
        {
            // Weight (base = kg)
            Unit.Create("KG", "Kilogram", UnitDimension.Weight, 1m, "কেজি"),
            Unit.Create("GM", "Gram", UnitDimension.Weight, 0.001m, "গ্রাম"),
            Unit.Create("SEER", "Seer", UnitDimension.Weight, 0.9331m, "সের"),
            Unit.Create("MON", "Mon (Maund)", UnitDimension.Weight, 37.3242m, "মন"),

            // Volume (base = litre)
            Unit.Create("LTR", "Litre", UnitDimension.Volume, 1m, "লিটার"),
            Unit.Create("ML", "Millilitre", UnitDimension.Volume, 0.001m, "মিলি"),

            // Count (base = piece)
            Unit.Create("PCS", "Piece", UnitDimension.Count, 1m, "পিস"),
            Unit.Create("HALI", "Hali (4 pcs)", UnitDimension.Count, 4m, "হালি"),
            Unit.Create("DOZEN", "Dozen (12 pcs)", UnitDimension.Count, 12m, "ডজন"),
        };

        _db.Units.AddRange(units);
        await _db.SaveChangesAsync();

        _logger.LogInformation("UnitSeeder: seeded {Count} units.", units.Length);
    }
}
