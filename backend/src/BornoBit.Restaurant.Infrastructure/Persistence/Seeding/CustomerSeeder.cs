using BornoBit.Restaurant.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class CustomerSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomerSeeder> _logger;

    public CustomerSeeder(ApplicationDbContext db, ILogger<CustomerSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Customers.AnyAsync(c => c.Phone == Customer.WalkInPhone, cancellationToken)) return;

        _db.Customers.Add(Customer.Create(Customer.WalkInPhone, "Walk-in"));
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CustomerSeeder: seeded walk-in customer.");
    }
}
