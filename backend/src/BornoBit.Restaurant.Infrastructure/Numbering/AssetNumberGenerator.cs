using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Infrastructure.Numbering;

// Date-based fixed-asset numbering: FA-yyyyMMdd-NNNN, sequence per day. Mirrors JournalNumberGenerator.
public class AssetNumberGenerator : IAssetNumberGenerator
{
    private readonly ApplicationDbContext _db;

    public AssetNumberGenerator(ApplicationDbContext db) => _db = db;

    public async Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var prefix = $"FA-{nowUtc.Date:yyyyMMdd}-";
        var countToday = await _db.FixedAssets
            .IgnoreQueryFilters()
            .CountAsync(a => a.AssetNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{countToday + 1:D4}";
    }
}
