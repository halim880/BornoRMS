using BornoBit.Restaurant.Domain.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the consuming departments the store issues stock to. Cost centres only — no per-department stock.
/// Idempotent: skips if any department already exists.
/// </summary>
public class StoreDepartmentSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<StoreDepartmentSeeder> _logger;

    public StoreDepartmentSeeder(ApplicationDbContext db, ILogger<StoreDepartmentSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _db.StoreDepartments.AnyAsync()) return;

        var departments = new[]
        {
            StoreDepartment.Create("KITCHEN", "Kitchen", 1, "রান্নাঘর"),
            StoreDepartment.Create("BAR", "Bar", 2, "বার"),
            StoreDepartment.Create("BAKERY", "Bakery", 3, "বেকারি"),
            StoreDepartment.Create("HOUSEKEEPING", "Housekeeping", 4, "হাউসকিপিং"),
            StoreDepartment.Create("SERVICE", "Service (Front-of-house)", 5, "সার্ভিস"),
            StoreDepartment.Create("MAINTENANCE", "Maintenance", 6, "রক্ষণাবেক্ষণ"),
        };

        _db.StoreDepartments.AddRange(departments);
        await _db.SaveChangesAsync();

        _logger.LogInformation("StoreDepartmentSeeder: seeded {Count} store departments.", departments.Length);
    }
}
