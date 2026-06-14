using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Infrastructure.Numbering;

// Date-based drawer-shift numbering: DRW-yyyyMMdd-NNNN, sequence per day. Counts by number prefix
// (ignoring soft-delete) so a removed shift never frees a number that would collide with the unique
// index. Mirrors TransactionNumberGenerator.
public class DrawerNumberGenerator : IDrawerNumberGenerator
{
    private readonly ApplicationDbContext _db;

    public DrawerNumberGenerator(ApplicationDbContext db) => _db = db;

    public async Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var prefix = $"DRW-{nowUtc.Date:yyyyMMdd}-";

        var countToday = await _db.CashDrawerSessions
            .IgnoreQueryFilters()
            .CountAsync(d => d.DrawerNumber.StartsWith(prefix), cancellationToken);

        var seq = countToday + 1;
        return $"{prefix}{seq:D4}";
    }
}
