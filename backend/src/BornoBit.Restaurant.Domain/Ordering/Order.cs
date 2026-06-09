using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Ordering;

public class Order : AuditableEntity
{
    public string OrderNumber { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public Guid? RestaurantTableId { get; private set; }
    public OrderType OrderType { get; private set; }
    public DateTime OrderedAtUtc { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Placed;
    public string Currency { get; private set; } = "Tk";
    public string? Notes { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

    // Billing
    public decimal DiscountAmount { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public string? DiscountReason { get; private set; }
    public bool IsPaid { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public decimal? AmountTendered { get; private set; }
    public decimal? ChangeGiven { get; private set; }

    private readonly List<OrderLine> _lines = new();
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public decimal Subtotal => _lines.Sum(l => l.LineTotal);
    public decimal GrandTotal => Math.Max(0m, Subtotal - DiscountAmount);
    public decimal Total => GrandTotal;

    private Order() { }

    public static Order Create(
        string orderNumber,
        Guid customerId,
        Guid? restaurantTableId,
        OrderType orderType,
        DateTime orderedAtUtc,
        string currency = "Tk",
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("Order number is required.", nameof(orderNumber));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required.", nameof(customerId));
        if (orderType == OrderType.DineIn && restaurantTableId is null)
            throw new ArgumentException("Dine-in orders require a table.", nameof(restaurantTableId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        return new Order
        {
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            CustomerId = customerId,
            RestaurantTableId = restaurantTableId,
            OrderType = orderType,
            OrderedAtUtc = orderedAtUtc,
            Currency = currency.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = OrderStatus.Placed
        };
    }

    public OrderLine AddLine(Guid menuItemId, string code, string name, decimal unitPrice, string currency, int quantity = 1)
    {
        if (menuItemId == Guid.Empty) throw new ArgumentException("Menu item is required.", nameof(menuItemId));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));

        var line = new OrderLine
        {
            OrderId = Id,
            MenuItemId = menuItemId,
            Code = code,
            Name = name,
            UnitPriceSnapshot = unitPrice,
            Currency = currency.Trim(),
            Quantity = quantity
        };
        _lines.Add(line);
        return line;
    }

    public void Confirm() => TransitionTo(OrderStatus.Confirmed, expected: OrderStatus.Placed);
    public void StartPreparing() => TransitionTo(OrderStatus.Preparing, expected: OrderStatus.Confirmed);
    public void MarkReady() => TransitionTo(OrderStatus.Ready, expected: OrderStatus.Preparing);
    public void MarkServed() => TransitionTo(OrderStatus.Served, expected: OrderStatus.Ready);
    public void Complete() => TransitionTo(OrderStatus.Completed, expected: OrderStatus.Served);

    public void Cancel(string? reason)
    {
        if (Status == OrderStatus.Cancelled) throw new InvalidOperationException("Order is already cancelled.");
        if (Status == OrderStatus.Served || Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel an order that has already been served.");
        Status = OrderStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void UpdateNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    public void ApplyDiscount(decimal? percent, decimal? amount, string? reason)
    {
        if (IsPaid) throw new InvalidOperationException("Cannot change discount on a paid order.");
        if (Status == OrderStatus.Cancelled) throw new InvalidOperationException("Cannot discount a cancelled order.");

        decimal computed;
        if (percent is { } p)
        {
            if (p < 0m || p > 100m) throw new ArgumentOutOfRangeException(nameof(percent), "Discount percent must be between 0 and 100.");
            computed = Math.Round(Subtotal * p / 100m, 2);
            DiscountPercent = p;
        }
        else if (amount is { } a)
        {
            computed = a;
            DiscountPercent = null;
        }
        else
        {
            computed = 0m;
            DiscountPercent = null;
        }

        if (computed < 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Discount cannot be negative.");
        if (computed > Subtotal) throw new InvalidOperationException("Discount cannot exceed the subtotal.");

        DiscountAmount = computed;
        DiscountReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void RecordPayment(PaymentMethod method, decimal tendered)
    {
        if (IsPaid) throw new InvalidOperationException("Order is already paid.");
        if (Status == OrderStatus.Cancelled) throw new InvalidOperationException("Cannot pay a cancelled order.");
        if (Status == OrderStatus.Completed) throw new InvalidOperationException("Order is already completed.");

        var due = GrandTotal;
        if (tendered < due) throw new InvalidOperationException("Amount tendered is less than the amount due.");

        PaymentMethod = method;
        AmountTendered = tendered;
        ChangeGiven = tendered - due;
        IsPaid = true;
        PaidAtUtc = DateTime.UtcNow;
    }

    private void TransitionTo(OrderStatus next, OrderStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Cannot move from {Status} to {next}; expected current status {expected}.");
        Status = next;
    }
}
