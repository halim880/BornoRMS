using BornoBit.Restaurant.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class TenantSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TenantSeeder> _logger;

    public TenantSeeder(ApplicationDbContext db, ILogger<TenantSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Tenants.IgnoreQueryFilters().AnyAsync(cancellationToken)) return;

        var tenant = Tenant.Create("BornoBit Restaurant", "bornobit", "admin@bornobit.local");
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("TenantSeeder: seeded default tenant.");
    }
}
