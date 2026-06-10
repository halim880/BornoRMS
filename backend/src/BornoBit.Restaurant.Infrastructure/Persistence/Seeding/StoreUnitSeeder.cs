using BornoBit.Restaurant.Domain.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds store/warehouse units of measure (isolated from the POS UnitSeeder). Base units (factor 1):
/// KG (weight), LTR (volume), PCS (count). BD units: 1 seer = 0.9331 kg, 1 mon = 40 seer = 37.3242 kg,
/// 1 hali = 4 pcs, 1 dozen = 12 pcs.
/// </summary>
public class StoreUnitSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<StoreUnitSeeder> _logger;

    public StoreUnitSeeder(ApplicationDbContext db, ILogger<StoreUnitSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _db.StoreUnits.AnyAsync()) return;

        var units = new[]
        {
            // Weight (base = kg)
            StoreUnit.Create("KG", "Kilogram", StoreUnitDimension.Weight, 1m, "কেজি"),
            StoreUnit.Create("GM", "Gram", StoreUnitDimension.Weight, 0.001m, "গ্রাম"),
            StoreUnit.Create("SEER", "Seer", StoreUnitDimension.Weight, 0.9331m, "সের"),
            StoreUnit.Create("MON", "Mon (Maund)", StoreUnitDimension.Weight, 37.3242m, "মন"),
            StoreUnit.Create("BOSTA", "Bosta (50 kg)", StoreUnitDimension.Weight, 50m, "বস্তা"),

            // Volume (base = litre)
            StoreUnit.Create("LTR", "Litre", StoreUnitDimension.Volume, 1m, "লিটার"),
            StoreUnit.Create("ML", "Millilitre", StoreUnitDimension.Volume, 0.001m, "মিলি"),

            // Count (base = piece)
            StoreUnit.Create("PCS", "Piece", StoreUnitDimension.Count, 1m, "পিস"),
            StoreUnit.Create("HALI", "Hali (4 pcs)", StoreUnitDimension.Count, 4m, "হালি"),
            StoreUnit.Create("DOZEN", "Dozen (12 pcs)", StoreUnitDimension.Count, 12m, "ডজন"),
            StoreUnit.Create("CARTON", "Carton", StoreUnitDimension.Count, 1m, "কার্টন"),
        };

        _db.StoreUnits.AddRange(units);
        await _db.SaveChangesAsync();

        _logger.LogInformation("StoreUnitSeeder: seeded {Count} store units.", units.Length);
    }
}
