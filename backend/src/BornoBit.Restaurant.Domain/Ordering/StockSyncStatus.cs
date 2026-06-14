namespace BornoBit.Restaurant.Domain.Ordering;

/// <summary>
/// Tracks whether an order's stock has been deducted, so a failed deduction never silently loses
/// inventory and a background worker can retry. Orthogonal to <see cref="OrderStatus"/>.
/// </summary>
public enum StockSyncStatus
{
    /// <summary>Not yet attempted, or the order has no stock-impacting lines.</summary>
    NotApplicable = 0,
    /// <summary>Deduction in progress / queued for retry.</summary>
    Pending = 1,
    /// <summary>Ingredients/direct stock successfully deducted.</summary>
    Synced = 2,
    /// <summary>Deduction threw; the retry worker will re-attempt.</summary>
    Failed = 3,
    /// <summary>Order cancelled and a prior deduction was reversed (restored to stock).</summary>
    Reversed = 4
}
