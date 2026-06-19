namespace BornoBit.Restaurant.Domain.Logistics;

/// <summary>
/// Dispatch lifecycle of a delivery order, tracked independently of the order's own
/// <see cref="Ordering.OrderStatus"/> (which stays food-centric: Placed→…→Ready→Served).
/// </summary>
public enum DeliveryStatus
{
    Pending = 0,        // delivery order placed, no rider yet
    Assigned = 1,       // a rider has been assigned
    OutForDelivery = 2, // rider has picked up and left
    Delivered = 3,      // handed to the customer
    Failed = 4,         // customer rejected / could not deliver
    Cancelled = 5       // mirrors an order cancellation
}
