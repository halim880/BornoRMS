using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Dining;

/// <summary>
/// A dine-in visit at a table. Owns the guests, the assigned waiter and the orders placed during the
/// visit, and is the unit waiters transfer / merge / split / close. The running bill is the sum of the
/// session's orders' grand totals (computed in the query layer — order totals are not EF-translatable).
/// </summary>
public class DiningSession : AuditableEntity
{
    public string SessionNumber { get; private set; } = default!;
    public Guid RestaurantTableId { get; private set; }
    public Guid? WaiterUserId { get; private set; }
    public string? WaiterName { get; private set; }
    public int GuestCount { get; private set; }
    public DiningSessionStatus Status { get; private set; } = DiningSessionStatus.Open;

    public DateTime OpenedAtUtc { get; private set; }
    public DateTime LastActivityAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    /// <summary>When this session was absorbed into another (merge), the survivor's id.</summary>
    public Guid? MergedIntoSessionId { get; private set; }
    public string? CloseReason { get; private set; }

    private DiningSession() { }

    public static DiningSession Open(
        string sessionNumber,
        Guid restaurantTableId,
        int guestCount,
        DateTime openedAtUtc,
        Guid? waiterUserId = null,
        string? waiterName = null)
    {
        if (string.IsNullOrWhiteSpace(sessionNumber)) throw new ArgumentException("Session number is required.", nameof(sessionNumber));
        if (restaurantTableId == Guid.Empty) throw new ArgumentException("Table is required.", nameof(restaurantTableId));
        if (guestCount < 0) throw new ArgumentOutOfRangeException(nameof(guestCount));

        return new DiningSession
        {
            SessionNumber = sessionNumber.Trim().ToUpperInvariant(),
            RestaurantTableId = restaurantTableId,
            GuestCount = guestCount,
            WaiterUserId = waiterUserId,
            WaiterName = string.IsNullOrWhiteSpace(waiterName) ? null : waiterName.Trim(),
            Status = DiningSessionStatus.Open,
            OpenedAtUtc = openedAtUtc,
            LastActivityAtUtc = openedAtUtc
        };
    }

    /// <summary>Bumps last-activity (call after any order/billing action on the session).</summary>
    public void Touch() => LastActivityAtUtc = DateTime.UtcNow;

    public void ChangeGuestCount(int guestCount)
    {
        EnsureActive();
        if (guestCount < 0) throw new ArgumentOutOfRangeException(nameof(guestCount));
        GuestCount = guestCount;
        Touch();
    }

    public void TransferWaiter(Guid? waiterUserId, string? waiterName)
    {
        EnsureActive();
        WaiterUserId = waiterUserId;
        WaiterName = string.IsNullOrWhiteSpace(waiterName) ? null : waiterName.Trim();
        Touch();
    }

    public void MoveToTable(Guid restaurantTableId)
    {
        EnsureActive();
        if (restaurantTableId == Guid.Empty) throw new ArgumentException("Table is required.", nameof(restaurantTableId));
        RestaurantTableId = restaurantTableId;
        Touch();
    }

    /// <summary>Bill requested — flags the session so the floor shows it as awaiting payment.</summary>
    public void MarkBilling()
    {
        if (Status == DiningSessionStatus.Open) Status = DiningSessionStatus.Billing;
        else if (Status != DiningSessionStatus.Billing)
            throw new InvalidOperationException($"Cannot request billing on a {Status} session.");
        Touch();
    }

    public void Close(string? reason)
    {
        if (Status is DiningSessionStatus.Closed or DiningSessionStatus.Merged)
            throw new InvalidOperationException($"Session is already {Status}.");
        Status = DiningSessionStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
        CloseReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>Marks this session absorbed into <paramref name="survivorSessionId"/> (its orders are re-parented by the handler).</summary>
    public void MergeInto(Guid survivorSessionId)
    {
        if (Status is DiningSessionStatus.Closed or DiningSessionStatus.Merged)
            throw new InvalidOperationException($"Cannot merge a {Status} session.");
        if (survivorSessionId == Guid.Empty) throw new ArgumentException("Survivor session is required.", nameof(survivorSessionId));
        if (survivorSessionId == Id) throw new InvalidOperationException("A session cannot merge into itself.");
        Status = DiningSessionStatus.Merged;
        MergedIntoSessionId = survivorSessionId;
        ClosedAtUtc = DateTime.UtcNow;
    }

    private void EnsureActive()
    {
        if (Status is DiningSessionStatus.Closed or DiningSessionStatus.Merged)
            throw new InvalidOperationException($"Cannot modify a {Status} session.");
    }
}
