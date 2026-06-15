using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>Collections over a date range (UTC), grouped by payment method.
/// Range version of <see cref="GetCashSummaryQuery"/>.</summary>
public record GetCollectionReportQuery(DateTime From, DateTime To) : IRequest<CollectionReportDto>;

public record CollectionMethodLineDto(PaymentMethod Method, int Count, decimal Amount);

public record CollectionReportDto(
    IReadOnlyList<CollectionMethodLineDto> ByMethod,
    int TotalCount,
    decimal TotalCollected,
    string Currency);

public class GetCollectionReportQueryHandler : IRequestHandler<GetCollectionReportQuery, CollectionReportDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetCollectionReportQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CollectionReportDto> Handle(GetCollectionReportQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.RangeUtc(DateOnly.FromDateTime(request.From), DateOnly.FromDateTime(request.To));

        // Recompute Order.Total inline — it is a C# computed property EF cannot translate.
        var paid = await _db.Orders
            .Where(o => o.IsPaid
                        && o.Status != OrderStatus.Cancelled
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
            .Select(g => new CollectionMethodLineDto(g.Key, g.Count(), g.Sum(x => x.Total)))
            .OrderBy(l => l.Method)
            .ToList();

        var currency = paid.Select(p => p.Currency).FirstOrDefault() ?? "Tk";

        return new CollectionReportDto(
            byMethod,
            paid.Count,
            paid.Sum(p => p.Total),
            currency);
    }
}
