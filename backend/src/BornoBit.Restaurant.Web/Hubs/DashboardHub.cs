using Microsoft.AspNetCore.SignalR;

namespace BornoBit.Restaurant.Web.Hubs;

/// <summary>
/// Real-time signal channel for the Operations Dashboard. The hub deliberately carries NO data — it
/// only emits content-free "changed" ticks (with a scope hint). Connected dashboards react by
/// re-running their authenticated MediatR queries inside their own circuit, so no sensitive data ever
/// crosses the hub and the connection needs no special auth.
/// </summary>
public sealed class DashboardHub : Hub
{
    public const string ChangedEvent = "changed";
}
