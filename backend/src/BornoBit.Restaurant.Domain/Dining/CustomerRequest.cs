using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Dining;

public enum CustomerRequestType
{
    CallWaiter = 0,
    RequestBill = 1,
    NeedWater = 2,
    NeedTissue = 3
}

public enum CustomerRequestStatus
{
    Pending = 0,
    Resolved = 1
}

/// <summary>
/// A service request raised from a table (QR/customer site or staff): call waiter, request the bill,
/// need water/tissue. Drives the dashboard "Customer Requests" section; staff mark it resolved.
/// </summary>
public class CustomerRequest : AuditableEntity
{
    public Guid RestaurantTableId { get; private set; }
    public CustomerRequestType Type { get; private set; }
    public CustomerRequestStatus Status { get; private set; } = CustomerRequestStatus.Pending;
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? Note { get; private set; }

    private CustomerRequest() { }

    public static CustomerRequest Create(Guid restaurantTableId, CustomerRequestType type, DateTime requestedAtUtc, string? note = null)
    {
        if (restaurantTableId == Guid.Empty) throw new ArgumentException("Table is required.", nameof(restaurantTableId));

        return new CustomerRequest
        {
            RestaurantTableId = restaurantTableId,
            Type = type,
            Status = CustomerRequestStatus.Pending,
            RequestedAtUtc = requestedAtUtc,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }

    public void Resolve(string? resolvedBy)
    {
        if (Status == CustomerRequestStatus.Resolved) throw new InvalidOperationException("Request is already resolved.");
        Status = CustomerRequestStatus.Resolved;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolvedBy = string.IsNullOrWhiteSpace(resolvedBy) ? null : resolvedBy.Trim();
    }
}
