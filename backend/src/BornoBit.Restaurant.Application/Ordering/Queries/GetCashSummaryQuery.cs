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

        // Project total in SQL — Order.Total is a C# computed property over the lines
        // navigation, which EF cannot translate, so recompute it inline (mirrors GetOrdersQuery).
        var paid = await _db.Orders
            .Where(o => o.IsPaid
                        && o.PaidAtUtc != null
                        && o.PaidAtUtc >= start
                        && o.PaidAtUtc < end
                        && o.PaymentMethod != null)
            .Select(o => new
            {
                Method = o.PaymentMethod!.Value,
                Total = Math.Max(
                    0m,
                    (o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m)
                        - o.DiscountAmount + o.RoundingAdjustment),
                o.Currency
            })
            .ToListAsync(cancellationToken);

        var byMethod = paid
            .GroupBy(p => p.Method)
            .Select(g => new CashMethodLineDto(g.Key, g.Count(), g.Sum(x => x.Total)))
            .OrderBy(l => l.Method)
            .ToList();

        var currency = paid.Select(p => p.Currency).FirstOrDefault() ?? "Tk";

        return new CashSummaryDto(
            date,
            byMethod,
            paid.Count,
            paid.Sum(p => p.Total),
            currency);
    }
}
