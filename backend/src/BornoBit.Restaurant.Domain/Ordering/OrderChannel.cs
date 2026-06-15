namespace BornoBit.Restaurant.Domain.Ordering;

/// <summary>
/// Where an order originated. Drives the accept workflow: <see cref="Qr"/> and <see cref="Waiter"/>
/// orders are trusted in-house and auto-confirmed at placement (an accepted ticket immediately), while
/// <see cref="Online"/> and <see cref="Pos"/> orders stay <see cref="OrderStatus.Placed"/> until staff
/// explicitly accept them on the Kitchen Display.
/// </summary>
public enum OrderChannel
{
    /// <summary>Dine-in customer self-order via the table QR / customer site.</summary>
    Qr = 0,
    /// <summary>Punched by a waiter on the staff console.</summary>
    Waiter = 1,
    /// <summary>Built at the POS counter (created empty, lines added before it is sent to the kitchen).</summary>
    Pos = 2,
    /// <summary>Remote takeaway/delivery placed via the customer site; needs staff acceptance.</summary>
    Online = 3
}
