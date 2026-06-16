using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Infrastructure.Numbering;

// Month-based payroll-run numbering: PR-yyyyMM-NNNN, sequence per month.
public class PayrollNumberGenerator : IPayrollNumberGenerator
{
    private readonly ApplicationDbContext _db;

    public PayrollNumberGenerator(ApplicationDbContext db) => _db = db;

    public async Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var prefix = $"PR-{nowUtc:yyyyMM}-";
        var count = await _db.PayrollRuns
            .IgnoreQueryFilters()
            .CountAsync(r => r.RunNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:D4}";
    }
}
