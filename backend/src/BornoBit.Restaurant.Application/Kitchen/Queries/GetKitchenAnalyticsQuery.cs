using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Kitchen.Queries;

/// <summary>
/// Kitchen analytics over a date window: prep-time leaders/laggards, throughput, peak hours and the
/// most-delayed tickets. Prep duration is per-order (Ready − Preparing); item speed is attributed from
/// the prep time of the orders each item appeared in.
/// </summary>
public record GetKitchenAnalyticsQuery(DateOnly From, DateOnly To, int TopN = 5) : IRequest<KitchenAnalyticsDto>;

public record KitchenAnalyticsDto(
    double AveragePrepMinutes,
    int OrdersCompleted,
    IReadOnlyList<ItemSpeedDto> FastestItems,
    IReadOnlyList<ItemSpeedDto> SlowestItems,
    IReadOnlyList<HourLoadDto> PeakHours,
    IReadOnlyList<DelayedOrderDto> MostDelayed);

public record ItemSpeedDto(string Name, double AveragePrepMinutes, int TimesPrepared);
public record HourLoadDto(int Hour, int OrderCount);
public record DelayedOrderDto(string OrderNumber, double MinutesToReady, DateTime OrderedAtUtc);

public class GetKitchenAnalyticsQueryHandler : IRequestHandler<GetKitchenAnalyticsQuery, KitchenAnalyticsDto>
{
    private readonly IAppDbContext _db;

    public GetKitchenAnalyticsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<KitchenAnalyticsDto> Handle(GetKitchenAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var start = request.From.ToDateTime(TimeOnly.MinValue);
        var end = request.To.ToDateTime(TimeOnly.MinValue).AddDays(1);

        // Orders that reached Ready in the window, with their lines, for prep-time analytics.
        var prepped = await _db.Orders
            .Where(o => o.PreparingAtUtc != null && o.ReadyAtUtc != null
                        && o.ReadyAtUtc >= start && o.ReadyAtUtc < end
                        && o.Status != OrderStatus.Cancelled)
            .Select(o => new
            {
                o.OrderNumber,
                o.OrderedAtUtc,
                o.PreparingAtUtc,
                o.ReadyAtUtc,
                LineNames = o.Lines.Select(l => l.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        var withPrep = prepped
            .Select(o => new
            {
                o.OrderNumber,
                o.OrderedAtUtc,
                PrepMinutes = (o.ReadyAtUtc!.Value - o.PreparingAtUtc!.Value).TotalMinutes,
                ToReadyMinutes = (o.ReadyAtUtc!.Value - o.OrderedAtUtc).TotalMinutes,
                o.LineNames
            })
            .ToList();

        var avgPrep = withPrep.Count > 0 ? Math.Round(withPrep.Average(o => o.PrepMinutes), 1) : 0d;

        var itemSpeeds = withPrep
            .SelectMany(o => o.LineNames.Select(n => (Name: n, o.PrepMinutes)))
            .GroupBy(x => x.Name)
            .Select(g => new ItemSpeedDto(g.Key, Math.Round(g.Average(x => x.PrepMinutes), 1), g.Count()))
            .ToList();

        var fastest = itemSpeeds.OrderBy(x => x.AveragePrepMinutes).Take(request.TopN).ToList();
        var slowest = itemSpeeds.OrderByDescending(x => x.AveragePrepMinutes).Take(request.TopN).ToList();

        var mostDelayed = withPrep
            .OrderByDescending(o => o.ToReadyMinutes)
            .Take(request.TopN)
            .Select(o => new DelayedOrderDto(o.OrderNumber, Math.Round(o.ToReadyMinutes, 1), o.OrderedAtUtc))
            .ToList();

        // Peak hours: all orders placed in the window, grouped by hour-of-day.
        var placed = await _db.Orders
            .Where(o => o.OrderedAtUtc >= start && o.OrderedAtUtc < end && o.Status != OrderStatus.Cancelled)
            .Select(o => o.OrderedAtUtc.Hour)
            .ToListAsync(cancellationToken);

        var peakHours = placed
            .GroupBy(h => h)
            .Select(g => new HourLoadDto(g.Key, g.Count()))
            .OrderBy(x => x.Hour)
            .ToList();

        return new KitchenAnalyticsDto(avgPrep, withPrep.Count, fastest, slowest, peakHours, mostDelayed);
    }
}
