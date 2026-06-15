using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Infrastructure.Numbering;

// Date-based journal voucher numbering: JV-yyyyMMdd-NNNN, sequence per day. Counts by
// number prefix (ignoring soft-delete/void) so a removed draft never frees a number that
// would collide with the unique index. Mirrors OrderNumberGenerator.
public class JournalNumberGenerator : IJournalNumberGenerator
{
    private readonly ApplicationDbContext _db;

    public JournalNumberGenerator(ApplicationDbContext db) => _db = db;

    public async Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var prefix = $"JV-{nowUtc.Date:yyyyMMdd}-";

        var countToday = await _db.JournalEntries
            .IgnoreQueryFilters()
            .CountAsync(e => e.EntryNumber.StartsWith(prefix), cancellationToken);

        var seq = countToday + 1;
        return $"{prefix}{seq:D4}";
    }
}
