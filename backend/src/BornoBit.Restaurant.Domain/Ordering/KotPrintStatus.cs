namespace BornoBit.Restaurant.Domain.Ordering;

/// <summary>
/// Tracks whether the kitchen order ticket (KOT) has been dispatched to the print agent. Orthogonal to
/// the status workflow, mirroring <see cref="StockSyncStatus"/>: the dispatch is a best-effort side effect
/// of accepting an order, retried by a background worker so the kitchen never silently misses a ticket.
/// </summary>
public enum KotPrintStatus
{
    /// <summary>Not yet dispatched (order not accepted, or awaiting first dispatch).</summary>
    NotPrinted = 0,
    /// <summary>Dispatch in progress / queued for retry.</summary>
    Pending = 1,
    /// <summary>Successfully acknowledged by the print agent.</summary>
    Printed = 2,
    /// <summary>Dispatch failed; the retry worker will re-attempt.</summary>
    Failed = 3
}
