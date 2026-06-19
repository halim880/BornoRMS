using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BornoBit.Restaurant.Api.Hubs;

/// <summary>
/// Real-time signal channel for the Flutter staff console (POS / KDS / waiter / dashboard).
/// Mirrors the Blazor <c>DashboardHub</c>: the hub deliberately carries NO data — it only emits
/// content-free "changed" ticks tagged with a <see cref="LiveScopes">scope</see>. Connected clients
/// react by re-running their own authenticated REST queries, so no sensitive data crosses the hub.
/// Auth is the same staff JWT used for the REST API (bearer token, also read from the
/// <c>access_token</c> query string for the WebSocket handshake — see Program.cs).
/// </summary>
[Authorize(Policy = "Staff")]
public sealed class LiveHub : Hub
{
    public const string ChangedEvent = "changed";
}
