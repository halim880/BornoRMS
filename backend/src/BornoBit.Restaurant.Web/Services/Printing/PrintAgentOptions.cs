namespace BornoBit.Restaurant.Web.Services.Printing;

public sealed class PrintAgentOptions
{
    public const string SectionName = "PrintAgent";

    /// <summary>"Http" pushes to the agent's local API, "Hub" pushes through the SignalR
    /// hub the agent dials into (cloud-hosted web app), "Off" disables printing.</summary>
    public string Mode { get; set; } = "Http";

    /// <summary>Agent base URL for Http mode, e.g. http://127.0.0.1:9123.</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:9123";

    /// <summary>Sent as X-Api-Key (Http mode); also the key agents must present on the hub.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Which connected agent receives jobs in Hub mode.</summary>
    public string AgentId { get; set; } = "counter-1";

    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>Profile name/alias to target on the agent; null uses the agent default.</summary>
    public string? PrinterName { get; set; }

    public bool OpenCashDrawerOnCashPayment { get; set; } = true;

    /// <summary>Auto-dispatch the kitchen order ticket when an order is accepted/fired.</summary>
    public bool AutoPrintKot { get; set; } = true;

    /// <summary>Profile/alias for the kitchen printer; falls back to <see cref="PrinterName"/> when null.</summary>
    public string? KitchenPrinterName { get; set; }

    public bool IsOff => string.Equals(Mode, "Off", StringComparison.OrdinalIgnoreCase);
    public bool IsHub => string.Equals(Mode, "Hub", StringComparison.OrdinalIgnoreCase);
}
