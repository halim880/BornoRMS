using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using BornoBit.Restaurant.Web.Services.Printing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BornoBit.Restaurant.Web.Hubs;

/// <summary>
/// Dial-out transport for print agents that cannot accept inbound connections
/// (cloud-hosted web app). Agents connect with ?agentId= and an X-Agent-Key header;
/// the server pushes jobs with client-result invocations ("Print").
/// Authentication is key-based — cookie auth never applies to agents.
/// </summary>
public sealed class PrintHub(IOptions<PrintAgentOptions> options, ILogger<PrintHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, string> Agents = new(StringComparer.OrdinalIgnoreCase);

    public static string? GetConnectionId(string agentId) =>
        Agents.TryGetValue(agentId, out var connectionId) ? connectionId : null;

    public static IReadOnlyCollection<string> ConnectedAgents => Agents.Keys.ToArray();

    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        var agentId = http?.Request.Query["agentId"].ToString();
        var presentedKey = http?.Request.Headers["X-Agent-Key"].ToString();
        var expectedKey = options.Value.ApiKey;

        // Hub mode exists for non-local deployments, so a key is mandatory.
        if (string.IsNullOrWhiteSpace(agentId) ||
            string.IsNullOrWhiteSpace(expectedKey) ||
            !FixedTimeEquals(presentedKey ?? "", expectedKey))
        {
            logger.LogWarning("Rejected print-agent connection (agentId: '{AgentId}', key valid: false)", agentId);
            Context.Abort();
            return;
        }

        Agents[agentId] = Context.ConnectionId;
        logger.LogInformation("Print agent '{AgentId}' connected ({ConnectionId})", agentId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var entry in Agents.Where(a => a.Value == Context.ConnectionId).ToList())
        {
            Agents.TryRemove(entry);
            logger.LogInformation("Print agent '{AgentId}' disconnected", entry.Key);
        }
        await base.OnDisconnectedAsync(exception);
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
