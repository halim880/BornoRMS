using System.Runtime.Versioning;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BornoBit.PrintAgent;

/// <summary>
/// Dials OUT to the server's <c>/hubs/print</c> SignalR hub (so the restaurant PC needs no inbound
/// firewall rule), authenticates with the shared key, and answers the server's "Print" client-result
/// invocations. Auto-reconnects forever; a dropped link is normal when the PC sleeps or wifi blips.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class HubWorker(
    IOptions<PrintAgentConfig> options,
    JobProcessor processor,
    ILogger<HubWorker> logger) : BackgroundService
{
    private readonly PrintAgentConfig _cfg = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_cfg.ApiKey))
        {
            logger.LogError("PrintAgent:ApiKey is empty - the hub will reject this agent. Set it in appsettings.json.");
            return;
        }

        var url = BuildUrl(_cfg.HubUrl, _cfg.AgentId);
        var connection = new HubConnectionBuilder()
            .WithUrl(url, opts => opts.Headers["X-Agent-Key"] = _cfg.ApiKey)
            .WithAutomaticReconnect(new ForeverRetry())
            .Build();

        // Server pushes jobs as client-result invocations: InvokeAsync<PrintJobResponse>("Print", request).
        connection.On<PrintJobRequest, PrintJobResponse>("Print", req =>
        {
            logger.LogInformation("Received job {JobId} (order {OrderId}).", req.JobId, req.OrderId);
            return Task.FromResult(processor.Process(req));
        });

        connection.Reconnecting += err => { logger.LogWarning(err, "Hub connection lost — reconnecting…"); return Task.CompletedTask; };
        connection.Reconnected += id => { logger.LogInformation("Reconnected to hub ({ConnectionId}).", id); return Task.CompletedTask; };
        connection.Closed += async err =>
        {
            logger.LogWarning(err, "Hub connection closed — retrying in 5s.");
            await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
            await ConnectWithRetryAsync(connection, stoppingToken);
        };

        logger.LogInformation("Print agent '{AgentId}' starting; hub = {Url}", _cfg.AgentId, url);
        await ConnectWithRetryAsync(connection, stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { /* shutting down */ }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private async Task ConnectWithRetryAsync(HubConnection connection, CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(2);
        while (!ct.IsCancellationRequested && connection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await connection.StartAsync(ct);
                logger.LogInformation("Connected to hub as '{AgentId}'.", _cfg.AgentId);
                return;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("Connect failed ({Message}); retrying in {Delay}s.", ex.Message, delay.TotalSeconds);
                try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { return; }
                delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
            }
        }
    }

    private static string BuildUrl(string hubUrl, string agentId)
    {
        var sep = hubUrl.Contains('?') ? '&' : '?';
        return $"{hubUrl}{sep}agentId={Uri.EscapeDataString(agentId)}";
    }

    /// <summary>Never give up reconnecting — capped at 30s between attempts.</summary>
    private sealed class ForeverRetry : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext context) =>
            TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, Math.Min(5, context.PreviousRetryCount))));
    }
}
