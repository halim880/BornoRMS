using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Web.Services.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Web.Services.Stock;

/// <summary>
/// Failure-recovery worker: re-attempts stock deduction for orders left <see cref="StockSyncStatus.Failed"/>
/// (or stuck <see cref="StockSyncStatus.Pending"/>) — e.g. a transient DB error during confirm. Mirrors
/// <see cref="DashboardPollingService"/>. The consumption engine's idempotency guard makes re-runs safe.
/// </summary>
public sealed class StockSyncRetryService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PendingGrace = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDashboardNotifier _notifier;
    private readonly ILogger<StockSyncRetryService> _logger;

    public StockSyncRetryService(
        IServiceScopeFactory scopeFactory, IDashboardNotifier notifier, ILogger<StockSyncRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await ReconcileAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogDebug(ex, "Stock-sync retry sweep failed; will retry."); }
        }
    }

    private async Task ReconcileAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var consumption = scope.ServiceProvider.GetRequiredService<IStockConsumptionService>();

        var pendingCutoff = DateTime.UtcNow - PendingGrace;

        var stuck = await db.Orders
            .Include(o => o.Lines)
            .Where(o =>
                (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing
                 || o.Status == OrderStatus.Ready || o.Status == OrderStatus.Served)
                && (o.StockSyncStatus == StockSyncStatus.Failed
                    || (o.StockSyncStatus == StockSyncStatus.Pending
                        && (o.UpdatedAtUtc ?? o.CreatedAtUtc) < pendingCutoff)))
            .Take(50)
            .ToListAsync(ct);

        if (stuck.Count == 0) return;

        var recovered = 0;
        foreach (var order in stuck)
        {
            try
            {
                await consumption.ApplyConsumptionAsync(db, order, ct);
                await db.SaveChangesAsync(ct);
                recovered++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stock-sync retry still failing for order {OrderId}.", order.Id);
            }
        }

        if (recovered > 0)
        {
            _logger.LogInformation("Stock-sync retry recovered {Count} order(s).", recovered);
            await _notifier.NotifyAsync(DashboardScopes.Inventory, ct);
        }
    }
}
