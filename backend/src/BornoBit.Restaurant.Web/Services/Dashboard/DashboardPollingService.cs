using BornoBit.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BornoBit.Restaurant.Web.Services.Dashboard;

/// <summary>
/// Bridges cross-process changes onto the dashboard. Orders and customer requests can be created in the
/// separate API process (customer QR flow), so an in-process event would miss them. This service polls a
/// cheap change-signature (counts + latest timestamps) every few seconds and emits a "changed" tick when
/// it moves. Web-process actions still push instantly via <see cref="IDashboardNotifier"/>; this is the
/// safety net that also carries API-originated events. No Redis backplane required.
/// </summary>
public sealed class DashboardPollingService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDashboardNotifier _notifier;
    private readonly ILogger<DashboardPollingService> _logger;
    private string? _lastSignature;

    public DashboardPollingService(
        IServiceScopeFactory scopeFactory,
        IDashboardNotifier notifier,
        ILogger<DashboardPollingService> logger)
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
            try
            {
                var signature = await ComputeSignatureAsync(stoppingToken);
                if (_lastSignature is not null && signature != _lastSignature)
                    await _notifier.NotifyAsync(DashboardScopes.All, stoppingToken);
                _lastSignature = signature;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Dashboard poll failed; will retry.");
            }
        }
    }

    private async Task<string> ComputeSignatureAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var orderCount = await db.Orders.CountAsync(ct);
        var orderCreated = await db.Orders.MaxAsync(o => (DateTime?)o.CreatedAtUtc, ct);
        var orderUpdated = await db.Orders.MaxAsync(o => o.UpdatedAtUtc, ct);
        var orderPaid = await db.Orders.MaxAsync(o => o.PaidAtUtc, ct);

        var requestCount = await db.CustomerRequests.CountAsync(ct);
        var requestLatest = await db.CustomerRequests.MaxAsync(r => (DateTime?)r.RequestedAtUtc, ct);
        var requestUpdated = await db.CustomerRequests.MaxAsync(r => r.UpdatedAtUtc, ct);

        var sessionCount = await db.DiningSessions.CountAsync(ct);
        var sessionOpened = await db.DiningSessions.MaxAsync(s => (DateTime?)s.OpenedAtUtc, ct);
        var sessionUpdated = await db.DiningSessions.MaxAsync(s => s.UpdatedAtUtc, ct);
        var sessionActivity = await db.DiningSessions.MaxAsync(s => (DateTime?)s.LastActivityAtUtc, ct);

        var paymentCount = await db.Payments.CountAsync(ct);
        var paymentLatest = await db.Payments.MaxAsync(p => (DateTime?)p.CreatedAtUtc, ct);
        var drawerCount = await db.CashDrawerSessions.CountAsync(ct);
        var drawerUpdated = await db.CashDrawerSessions.MaxAsync(d => d.UpdatedAtUtc, ct);

        // Stock movements (incl. API-originated order consumption) + projection updates.
        var movementCount = await db.StockMovements.CountAsync(ct);
        var movementLatest = await db.StockMovements.MaxAsync(m => (DateTime?)m.OccurredAtUtc, ct);
        var projectionUpdated = await db.StockProjections.MaxAsync(p => (DateTime?)p.LastUpdatedUtc, ct);

        return string.Join('|',
            orderCount, orderCreated?.Ticks, orderUpdated?.Ticks, orderPaid?.Ticks,
            requestCount, requestLatest?.Ticks, requestUpdated?.Ticks,
            sessionCount, sessionOpened?.Ticks, sessionUpdated?.Ticks, sessionActivity?.Ticks,
            paymentCount, paymentLatest?.Ticks, drawerCount, drawerUpdated?.Ticks,
            movementCount, movementLatest?.Ticks, projectionUpdated?.Ticks);
    }
}
