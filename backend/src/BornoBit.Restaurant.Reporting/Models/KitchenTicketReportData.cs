namespace BornoBit.Restaurant.Reporting.Models;

/// <summary>
/// Data for a Kitchen Order Ticket (KOT) — the cook's work order. Carries items, quantities
/// and prep notes only; no prices, since the kitchen does not handle money.
/// </summary>
public record KitchenTicketReportData(
    string OrderNumber,
    DateTime OrderedAtUtc,
    string OrderType,
    string? TableNumber,
    string? CustomerName,
    string? Notes,
    DateTime GeneratedAtUtc,
    IReadOnlyList<KitchenTicketLine> Lines,
    string? CashierName = null,
    string? TicketLabel = null,
    ReceiptBranding? Branding = null
);

public record KitchenTicketLine(
    string Name,
    int Quantity,
    string? Notes = null,
    IReadOnlyList<string>? Modifiers = null
);
