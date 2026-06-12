using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Infrastructure.Numbering;

// Date-based finance transaction numbering: TXN-yyyyMMdd-NNNN, sequence per day. Counts by
// number prefix (ignoring soft-delete) so a removed transaction never frees a number that
// would collide with the unique index. Mirrors OrderNumberGenerator.
public class TransactionNumberGenerator : ITransactionNumberGenerator
{
    private readonly ApplicationDbContext _db;

    public TransactionNumberGenerator(ApplicationDbContext db) => _db = db;

    public async Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var prefix = $"TXN-{nowUtc.Date:yyyyMMdd}-";

        var countToday = await _db.FinanceTransactions
            .IgnoreQueryFilters()
            .CountAsync(t => t.Number.StartsWith(prefix), cancellationToken);

        var seq = countToday + 1;
        return $"{prefix}{seq:D4}";
    }
}
