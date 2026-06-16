namespace BornoBit.PrintAgent;

/// <summary>
/// Agent-side configuration, bound from the "PrintAgent" section of appsettings.json.
/// The <see cref="ApiKey"/> and <see cref="AgentId"/> MUST match the server's PrintAgent config
/// (the hub rejects connections whose key or agentId don't line up).
/// </summary>
public sealed class PrintAgentConfig
{
    public const string SectionName = "PrintAgent";

    /// <summary>Full hub URL, e.g. http://bornobit.innovatixinfosys.com/hubs/print.</summary>
    public string HubUrl { get; set; } = "http://bornobit.innovatixinfosys.com/hubs/print";

    /// <summary>Must equal the server's PrintAgent:AgentId (default "counter-1").</summary>
    public string AgentId { get; set; } = "counter-1";

    /// <summary>Shared secret; sent as the X-Agent-Key header. Must equal the server's PrintAgent:ApiKey.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Windows printer name for receipts. Empty = the system default printer.</summary>
    public string ReceiptPrinterName { get; set; } = "";

    /// <summary>Windows printer name for kitchen tickets. Empty = falls back to <see cref="ReceiptPrinterName"/>.</summary>
    public string KitchenPrinterName { get; set; } = "";

    /// <summary>Characters per line for the paper roll: 48 for 80mm, 32 for 58mm.</summary>
    public int PaperWidthChars { get; set; } = 48;

    /// <summary>"Printer" sends to the Windows spooler; "File" writes each job to disk for testing
    /// (no printer needed). NOT for production — "Microsoft Print to PDF" cannot accept raw ESC/POS.</summary>
    public string OutputMode { get; set; } = "Printer";

    /// <summary>Folder for "File" output mode. Empty = %TEMP%\BornoBitPrintAgent.</summary>
    public string OutputFolder { get; set; } = "";

    public bool IsFileMode => string.Equals(OutputMode, "File", StringComparison.OrdinalIgnoreCase);

    /// <summary>Honour the server's OpenCashDrawer flag (set false to never kick the drawer from this PC).</summary>
    public bool AllowCashDrawer { get; set; } = true;

    /// <summary>Full cut (true) vs partial cut (false) at the end of a job.</summary>
    public bool FullCut { get; set; } = false;
}
