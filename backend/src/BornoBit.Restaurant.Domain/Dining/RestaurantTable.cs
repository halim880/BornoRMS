using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Dining;

public class RestaurantTable : AuditableEntity
{
    public string TableNumber { get; private set; } = default!;
    public int Capacity { get; private set; }
    public bool IsActive { get; private set; } = true;

    // ----- short-lived edit hold (prevents two terminals grabbing the same dine-in table) -----
    public Guid? HeldByUserId { get; private set; }
    public string? HeldByName { get; private set; }
    public DateTime? HeldUntilUtc { get; private set; }

    private RestaurantTable() { }

    /// <summary>True when an unexpired hold by someone other than <paramref name="userId"/> is in force.</summary>
    public bool IsHeldByOther(Guid? userId, DateTime nowUtc) =>
        HeldUntilUtc is { } until && until > nowUtc && HeldByUserId != userId;

    /// <summary>
    /// Acquire (or refresh) a hold for the cashier/waiter editing this table. Throws if another terminal
    /// holds it and that hold has not yet expired.
    /// </summary>
    public void Hold(Guid userId, string? name, DateTime nowUtc, TimeSpan duration)
    {
        if (IsHeldByOther(userId, nowUtc))
            throw new InvalidOperationException($"Table {TableNumber} is being used by {HeldByName ?? "another terminal"}.");

        HeldByUserId = userId;
        HeldByName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        HeldUntilUtc = nowUtc.Add(duration);
    }

    /// <summary>Release the hold (on close/cancel). No-op if it is held by another still-active terminal.</summary>
    public void ReleaseHold(Guid? userId, DateTime nowUtc)
    {
        if (IsHeldByOther(userId, nowUtc)) return;
        HeldByUserId = null;
        HeldByName = null;
        HeldUntilUtc = null;
    }

    public static RestaurantTable Create(string tableNumber, int capacity)
    {
        if (string.IsNullOrWhiteSpace(tableNumber)) throw new ArgumentException("Table number is required.", nameof(tableNumber));
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");

        return new RestaurantTable
        {
            TableNumber = tableNumber.Trim(),
            Capacity = capacity,
            IsActive = true
        };
    }

    public void UpdateDetails(string tableNumber, int capacity)
    {
        if (string.IsNullOrWhiteSpace(tableNumber)) throw new ArgumentException("Table number is required.", nameof(tableNumber));
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        TableNumber = tableNumber.Trim();
        Capacity = capacity;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
