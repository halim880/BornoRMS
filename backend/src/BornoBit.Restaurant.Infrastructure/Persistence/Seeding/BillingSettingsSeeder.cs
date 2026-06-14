using BornoBit.Restaurant.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the single restaurant-wide billing settings row (VAT/service defaults). Idempotent —
/// skips once a row exists.
/// </summary>
public class BillingSettingsSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BillingSettingsSeeder> _logger;

    public BillingSettingsSeeder(ApplicationDbContext db, ILogger<BillingSettingsSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.RestaurantBillingSettings.AnyAsync(cancellationToken)) return;

        _db.RestaurantBillingSettings.Add(RestaurantBillingSettings.CreateDefault());
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("BillingSettingsSeeder: seeded default billing settings.");
    }
}
