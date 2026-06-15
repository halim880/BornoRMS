using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Ordering.Printing;
using BornoBit.Restaurant.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Web.Services.Stock;

/// <summary>
/// Dispatches kitchen tickets that the synchronous accept path could not deliver — chiefly QR orders
/// auto-confirmed in the API (no print agent there), plus anything left <see cref="KotPrintStatus.Failed"/>
/// by a transient agent outage. Mirrors <see cref="StockSyncRetryService"/>. Idempotent: the print agent
/// dedupes by job, and a Printed order is never re-scanned.
/// </summary>
public sealed class KotPrintRetryService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PendingGrace = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KotPrintRetryService> _logger;

    public KotPrintRetryService(IServiceScopeFactory scopeFactory, ILogger<KotPrintRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await ReconcileAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogDebug(ex, "KOT-print retry sweep failed; will retry."); }
        }
    }

    private async Task ReconcileAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IKitchenTicketSender>();

        var pendingCutoff = DateTime.UtcNow - PendingGrace;

        // Accepted (Confirmed+) but not-yet-printed orders. Placed orders are not accepted, so no ticket.
        var stuck = await db.Orders
            .Include(o => o.Lines)
            .Where(o =>
                (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing
                 || o.Status == OrderStatus.Ready || o.Status == OrderStatus.Served)
                && (o.KotPrintStatus == KotPrintStatus.NotPrinted
                    || o.KotPrintStatus == KotPrintStatus.Failed
                    || (o.KotPrintStatus == KotPrintStatus.Pending
                        && (o.UpdatedAtUtc ?? o.CreatedAtUtc) < pendingCutoff)))
            .Take(50)
            .ToListAsync(ct);

        if (stuck.Count == 0) return;

        var printed = 0;
        foreach (var order in stuck)
        {
            try
            {
                if (await sender.SendAsync(order, ct))
                {
                    order.MarkKotPrinted();
                    printed++;
                }
                else
                {
                    order.MarkKotFailed();
                }
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "KOT-print retry still failing for order {OrderId}.", order.Id);
            }
        }

        if (printed > 0)
            _logger.LogInformation("KOT-print retry dispatched {Count} ticket(s).", printed);
    }
}
