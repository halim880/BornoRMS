namespace BornoBit.Restaurant.Domain.Ordering;

/// <summary>
/// Order-level payment lifecycle, derived from the captured <see cref="Payment"/> rows.
/// Stored as a denormalised column on <see cref="Order"/> so the cash counter can filter/index it
/// without loading the payments collection.
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Refunded = 3,
    Cancelled = 4
}
