using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Infrastructure.Numbering;

// Minimal date-based order numbering: ORD-yyyyMMdd-N where N is the next
// sequence for that day. Good enough for the slice; a dedicated sequence
// table can replace this later if higher concurrency guarantees are needed.
public class OrderNumberGenerator : IOrderNumberGenerator
{
    private readonly IAppDbContext _db;

    public OrderNumberGenerator(IAppDbContext db) => _db = db;

    public async Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var dayStart = nowUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        var countToday = await _db.Orders
            .CountAsync(o => o.OrderedAtUtc >= dayStart && o.OrderedAtUtc < dayEnd, cancellationToken);

        var seq = countToday + 1;
        return $"ORD-{dayStart:yyyyMMdd}-{seq:D4}";
    }
}
