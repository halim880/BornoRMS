using BornoBit.Restaurant.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BornoBit.Restaurant.Api.Services;

/// <summary>Well-known scopes a "changed" tick can carry, so Flutter clients refresh selectively.</summary>
public static class LiveScopes
{
    public const string All = "all";
    public const string Orders = "orders";
    public const string Kitchen = "kitchen";
    public const string Requests = "requests";
    public const string Tables = "tables";
    public const string Sessions = "sessions";
    public const string Payments = "payments";
    public const string Inventory = "inventory";
    public const string Delivery = "delivery";
}

/// <summary>Fans a content-free "something changed" tick out to every connected staff client.</summary>
public interface ILiveNotifier
{
    Task NotifyAsync(string scope = LiveScopes.All, CancellationToken cancellationToken = default);
}

public sealed class LiveNotifier : ILiveNotifier
{
    private readonly IHubContext<LiveHub> _hub;
    private readonly ILogger<LiveNotifier> _logger;

    public LiveNotifier(IHubContext<LiveHub> hub, ILogger<LiveNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyAsync(string scope = LiveScopes.All, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients.All.SendAsync(LiveHub.ChangedEvent, scope, cancellationToken);
        }
        catch (Exception ex)
        {
            // A real-time tick is best-effort; clients also have a fallback poll, so never let a hub
            // hiccup fail the underlying mutation that already committed.
            _logger.LogWarning(ex, "Failed to broadcast live tick (scope={Scope}).", scope);
        }
    }
}
