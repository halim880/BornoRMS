namespace BornoBit.Restaurant.Domain.Dining;

/// <summary>
/// Lifecycle of a dine-in visit at a table. A session owns one or more orders placed during the visit.
/// </summary>
public enum DiningSessionStatus
{
    /// <summary>Guests seated; orders can be added.</summary>
    Open = 0,
    /// <summary>Bill requested / cashier settlement pending; no further ordering expected.</summary>
    Billing = 1,
    /// <summary>Visit finished, all orders settled.</summary>
    Closed = 2,
    /// <summary>Absorbed into another session (see <c>MergedIntoSessionId</c>); orders re-parented.</summary>
    Merged = 99
}
