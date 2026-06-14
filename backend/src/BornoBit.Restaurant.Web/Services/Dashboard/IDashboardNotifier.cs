using BornoBit.Restaurant.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BornoBit.Restaurant.Web.Services.Dashboard;

/// <summary>Well-known scopes a dashboard "changed" tick can carry, so clients can refresh selectively.</summary>
public static class DashboardScopes
{
    public const string All = "all";
    public const string Orders = "orders";
    public const string Requests = "requests";
    public const string Tables = "tables";
    public const string Sessions = "sessions";
    public const string Payments = "payments";
    public const string Inventory = "inventory";
}

/// <summary>Fans a content-free "something changed" tick out to every connected dashboard.</summary>
public interface IDashboardNotifier
{
    Task NotifyAsync(string scope = DashboardScopes.All, CancellationToken cancellationToken = default);
}

public sealed class DashboardNotifier : IDashboardNotifier
{
    private readonly IHubContext<DashboardHub> _hub;

    public DashboardNotifier(IHubContext<DashboardHub> hub) => _hub = hub;

    public Task NotifyAsync(string scope = DashboardScopes.All, CancellationToken cancellationToken = default) =>
        _hub.Clients.All.SendAsync(DashboardHub.ChangedEvent, scope, cancellationToken);
}
