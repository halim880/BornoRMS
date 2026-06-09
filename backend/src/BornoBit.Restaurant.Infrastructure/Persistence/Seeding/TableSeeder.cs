using BornoBit.Restaurant.Domain.Dining;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class TableSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TableSeeder> _logger;

    public TableSeeder(ApplicationDbContext db, ILogger<TableSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _db.RestaurantTables.AnyAsync()) return;

        for (var n = 1; n <= 10; n++)
        {
            var capacity = n <= 6 ? 4 : 6;
            _db.RestaurantTables.Add(RestaurantTable.Create($"T{n:D2}", capacity));
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("TableSeeder: seeded 10 tables.");
    }
}
