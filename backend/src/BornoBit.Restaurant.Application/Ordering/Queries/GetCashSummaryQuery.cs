using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>Takings for a single day (defaults to today, UTC), grouped by payment method.</summary>
public record GetCashSummaryQuery(DateOnly? Date = null) : IRequest<CashSummaryDto>;

public record CashMethodLineDto(PaymentMethod Method, int Count, decimal Amount);

public record CashSummaryDto(
    DateOnly Date,
    IReadOnlyList<CashMethodLineDto> ByMethod,
    int TotalCount,
    decimal TotalCollected,
    string Currency);

public class GetCashSummaryQueryHandler : IRequestHandler<GetCashSummaryQuery, CashSummaryDto>
{
    private readonly IAppDbContext _db;

    public GetCashSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CashSummaryDto> Handle(GetCashSummaryQuery request, CancellationToken cancellationToken)
    {
        var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);

        // Sourced from the payment ledger: captured tenders for the day, netting refunds, grouped by method.
        // Naturally reflects partial/split payments and the full grand total (incl. tax/service/tip).
        var payments = await _db.Payments
            .Where(p => p.Status == PaymentEntryStatus.Captured && p.CreatedAtUtc >= start && p.CreatedAtUtc < end)
            .Select(p => new { p.Method, p.Kind, p.Amount })
            .ToListAsync(cancellationToken);

        var byMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new CashMethodLineDto(
                g.Key,
                g.Count(x => x.Kind == PaymentKind.Charge),
                g.Sum(x => x.Kind == PaymentKind.Charge ? x.Amount : -x.Amount)))
            .OrderBy(l => l.Method)
            .ToList();

        return new CashSummaryDto(
            date,
            byMethod,
            byMethod.Sum(m => m.Count),
            byMethod.Sum(m => m.Amount),
            "Tk");
    }
}
