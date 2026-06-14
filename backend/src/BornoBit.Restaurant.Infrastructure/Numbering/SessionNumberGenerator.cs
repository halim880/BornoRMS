using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Infrastructure.Numbering;

// Date-based dining-session numbering: SES-yyyyMMdd-NNNN where NNNN is the next sequence for that day.
// Mirrors OrderNumberGenerator; a dedicated sequence table can replace this if higher concurrency
// guarantees become necessary.
public class SessionNumberGenerator : ISessionNumberGenerator
{
    private readonly IAppDbContext _db;

    public SessionNumberGenerator(IAppDbContext db) => _db = db;

    public async Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var dayStart = nowUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        var countToday = await _db.DiningSessions
            .CountAsync(s => s.OpenedAtUtc >= dayStart && s.OpenedAtUtc < dayEnd, cancellationToken);

        var seq = countToday + 1;
        return $"SES-{dayStart:yyyyMMdd}-{seq:D4}";
    }
}
