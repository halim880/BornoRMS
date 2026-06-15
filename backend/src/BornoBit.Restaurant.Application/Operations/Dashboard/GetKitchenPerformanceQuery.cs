using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Section-5 kitchen KPIs, computed from the per-status timestamps stamped on the order.</summary>
public record GetKitchenPerformanceQuery : IRequest<KitchenPerformanceDto>;

public record KitchenPerformanceDto(
    double AveragePrepMinutes,
    int OrdersWaitingOver10Min,
    int? LongestWaitingMinutes,
    string? LongestWaitingOrderNumber,
    int CompletedToday,
    int PendingCount,
    int PreparingCount,
    int ReadyCount);

public class GetKitchenPerformanceQueryHandler : IRequestHandler<GetKitchenPerformanceQuery, KitchenPerformanceDto>
{
    private const int DelayThresholdMinutes = 10;
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetKitchenPerformanceQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<KitchenPerformanceDto> Handle(GetKitchenPerformanceQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var (todayStart, tomorrow) = _clock.DayWindowUtc(_clock.Today);

        // Average prep time = Ready - Preparing, over orders that reached Ready today.
        var prepped = await _db.Orders
            .Where(o => o.PreparingAtUtc != null && o.ReadyAtUtc != null
                        && o.ReadyAtUtc >= todayStart && o.ReadyAtUtc < tomorrow)
            .Select(o => new { o.PreparingAtUtc, o.ReadyAtUtc })
            .ToListAsync(cancellationToken);
        var avgPrep = prepped.Count > 0
            ? Math.Round(prepped.Average(x => (x.ReadyAtUtc!.Value - x.PreparingAtUtc!.Value).TotalMinutes), 1)
            : 0d;

        // Open orders not yet served — feed waiting times + the kitchen load counts.
        var open = await _db.Orders
            .Where(o => !o.IsPaid && o.Status != OrderStatus.Cancelled
                        && (o.Status == OrderStatus.Placed || o.Status == OrderStatus.Confirmed
                            || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Ready))
            .Select(o => new { o.OrderNumber, o.OrderedAtUtc, o.Status })
            .ToListAsync(cancellationToken);

        // "Waiting" = not yet ready (still in the queue or being cooked).
        var notReady = open.Where(o => o.Status != OrderStatus.Ready).ToList();
        var waitingOver10 = notReady.Count(o => (now - o.OrderedAtUtc).TotalMinutes > DelayThresholdMinutes);
        var longest = notReady
            .OrderBy(o => o.OrderedAtUtc)
            .Select(o => new { o.OrderNumber, Minutes = (int)(now - o.OrderedAtUtc).TotalMinutes })
            .FirstOrDefault();

        var completedToday = await _db.Orders.CountAsync(
            o => o.Status == OrderStatus.Completed && o.PaidAtUtc != null
                 && o.PaidAtUtc >= todayStart && o.PaidAtUtc < tomorrow, cancellationToken);

        var pending = open.Count(o => o.Status is OrderStatus.Placed or OrderStatus.Confirmed);
        var preparing = open.Count(o => o.Status == OrderStatus.Preparing);
        var ready = open.Count(o => o.Status == OrderStatus.Ready);

        return new KitchenPerformanceDto(
            avgPrep,
            waitingOver10,
            longest?.Minutes,
            longest?.OrderNumber,
            completedToday,
            pending,
            preparing,
            ready);
    }
}
