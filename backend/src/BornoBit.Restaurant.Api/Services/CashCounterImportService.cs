using BornoBit.Restaurant.Application.Accounting.Transactions;
using BornoBit.Restaurant.Application.Common.Time;
using MediatR;

namespace BornoBit.Restaurant.Api.Services;

/// <summary>
/// Posts each business day's cash-counter takings to the General Ledger automatically, so the books never
/// silently drift behind the till when nobody runs the manual import. Fires once per day at a configured
/// local time and imports both the day that is ending and the previous day (to sweep up any stragglers
/// settled after the prior run). <see cref="ImportCashCounterCommand"/> is idempotent — already-accounted
/// invoices are skipped — so a re-run, an overlapping manual import, or a restart is always safe.
/// Configure via <c>CashImport:Enabled</c> (default true) and <c>CashImport:RunAtLocalTime</c> (default 23:55).
/// </summary>
public sealed class CashCounterImportService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<CashCounterImportService> _logger;

    public CashCounterImportService(IServiceScopeFactory scopes, IConfiguration config, ILogger<CashCounterImportService> logger)
    {
        _scopes = scopes;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("CashImport:Enabled", true))
        {
            _logger.LogInformation("Automatic cash-counter import is disabled (CashImport:Enabled=false).");
            return;
        }

        var runAt = ParseTime(_config["CashImport:RunAtLocalTime"]) ?? new TimeOnly(23, 55);
        _logger.LogInformation("Automatic cash-counter import scheduled daily at {RunAt} (local).", runAt);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNext(runAt);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var clock = scope.ServiceProvider.GetRequiredService<IBusinessClock>();

        // The day that is ending + the previous day, so takings settled after yesterday's run are swept up.
        foreach (var date in new[] { clock.Today, clock.Today.AddDays(-1) })
        {
            try
            {
                var result = await sender.Send(new ImportCashCounterCommand(date), cancellationToken);
                if (result.Count > 0)
                    _logger.LogInformation(
                        "Auto cash import {Date}: posted {Count} order(s), total {Total}. Skipped methods: {Skipped}.",
                        date, result.Count, result.Total, string.Join(", ", result.SkippedMethods));
            }
            catch (Exception ex)
            {
                // Never let one failed day kill the daily loop — log and carry on; next run retries (idempotent).
                _logger.LogError(ex, "Automatic cash-counter import failed for {Date}.", date);
            }
        }
    }

    private static TimeOnly? ParseTime(string? value) =>
        TimeOnly.TryParse(value, out var t) ? t : null;

    private static TimeSpan TimeUntilNext(TimeOnly runAt)
    {
        var now = DateTime.Now;
        var todayRun = now.Date + runAt.ToTimeSpan();
        var next = now < todayRun ? todayRun : todayRun.AddDays(1);
        return next - now;
    }
}
