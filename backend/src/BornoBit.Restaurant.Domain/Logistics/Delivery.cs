using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Logistics;

/// <summary>
/// Dispatch + tracking record for a single delivery order (1:1 with <see cref="Ordering.Order"/>).
/// Holds the structured address, the assigned rider, and the dispatch timestamps. Kept separate from
/// the Order aggregate so the order's food lifecycle and money math are untouched.
/// </summary>
public class Delivery : AuditableEntity
{
    public Guid OrderId { get; private set; }
    public Guid? RiderId { get; private set; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Pending;

    public string AddressLine { get; private set; } = default!;
    public string? ContactPhone { get; private set; }

    public DateTime? AssignedAtUtc { get; private set; }
    public DateTime? OutForDeliveryAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    private Delivery() { }

    public static Delivery Create(Guid orderId, string addressLine, string? contactPhone)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order is required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(addressLine)) throw new ArgumentException("Delivery address is required.", nameof(addressLine));

        return new Delivery
        {
            OrderId = orderId,
            AddressLine = addressLine.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim(),
            Status = DeliveryStatus.Pending
        };
    }

    public void UpdateAddress(string addressLine, string? contactPhone)
    {
        EnsureOpen();
        if (string.IsNullOrWhiteSpace(addressLine)) throw new ArgumentException("Delivery address is required.", nameof(addressLine));
        AddressLine = addressLine.Trim();
        ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
    }

    /// <summary>Assign (or re-assign) a rider. Allowed while Pending/Assigned — re-stamps the time.</summary>
    public void Assign(Guid riderId)
    {
        if (riderId == Guid.Empty) throw new ArgumentException("Rider is required.", nameof(riderId));
        if (Status is not (DeliveryStatus.Pending or DeliveryStatus.Assigned))
            throw new InvalidOperationException($"Cannot assign a rider to a {Status} delivery.");
        RiderId = riderId;
        Status = DeliveryStatus.Assigned;
        AssignedAtUtc = DateTime.UtcNow;
    }

    public void MarkOutForDelivery()
    {
        if (Status != DeliveryStatus.Assigned)
            throw new InvalidOperationException("Assign a rider before dispatch.");
        Status = DeliveryStatus.OutForDelivery;
        OutForDeliveryAtUtc = DateTime.UtcNow;
    }

    public void MarkDelivered()
    {
        if (Status != DeliveryStatus.OutForDelivery)
            throw new InvalidOperationException("Only an out-for-delivery order can be marked delivered.");
        Status = DeliveryStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
    }

    /// <summary>Marks the delivery failed (customer rejected / unreachable). Allowed before Delivered so
    /// the underlying order is still cancellable (it has not been Served).</summary>
    public void MarkFailed(string? reason)
    {
        if (Status is DeliveryStatus.Delivered or DeliveryStatus.Cancelled)
            throw new InvalidOperationException($"Cannot fail a {Status} delivery.");
        Status = DeliveryStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>Mirrors an order cancellation. Idempotent if already cancelled.</summary>
    public void Cancel()
    {
        if (Status == DeliveryStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a delivered order.");
        Status = DeliveryStatus.Cancelled;
    }

    private void EnsureOpen()
    {
        if (Status is DeliveryStatus.Delivered or DeliveryStatus.Cancelled)
            throw new InvalidOperationException($"Delivery is {Status}.");
    }
}
