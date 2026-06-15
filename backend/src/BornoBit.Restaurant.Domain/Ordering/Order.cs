using BornoBit.Restaurant.Domain.Common;
using PayMethod = BornoBit.Restaurant.Domain.Ordering.PaymentMethod;

namespace BornoBit.Restaurant.Domain.Ordering;

public class Order : AuditableEntity
{
    public string OrderNumber { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public Guid? RestaurantTableId { get; private set; }
    /// <summary>The dine-in visit this order belongs to. Null for takeaway/delivery or legacy orders.</summary>
    public Guid? DiningSessionId { get; private set; }
    public OrderType OrderType { get; private set; }
    /// <summary>Where this order originated. Drives the accept workflow (auto-confirm vs. explicit Accept).</summary>
    public OrderChannel Channel { get; private set; } = OrderChannel.Pos;
    public DateTime OrderedAtUtc { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Placed;
    public string Currency { get; private set; } = "Tk";
    public string? Notes { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

    // Kitchen / fulfilment timing — stamped as the order moves through the status workflow.
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? PreparingAtUtc { get; private set; }
    public DateTime? ReadyAtUtc { get; private set; }
    public DateTime? ServedAtUtc { get; private set; }
    /// <summary>Estimated ready time, computed on acceptance from the slowest line's prep time. Surfaced to the customer.</summary>
    public DateTime? EstimatedReadyAtUtc { get; private set; }

    // Service attribution (dashboard staff metrics + table occupancy).
    public Guid? WaiterUserId { get; private set; }
    public string? WaiterName { get; private set; }
    public int? GuestCount { get; private set; }

    // Kitchen Display
    /// <summary>Flagged by kitchen staff so the order sorts to the top of the board.</summary>
    public bool IsPriority { get; private set; }
    /// <summary>Internal kitchen notes (not customer-visible); distinct from the customer <see cref="Notes"/>.</summary>
    public string? KitchenNotes { get; private set; }

    // Billing
    public decimal DiscountAmount { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public string? DiscountReason { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal ServiceChargeAmount { get; private set; }
    /// <summary>Optional gratuity. Added to the payable but excluded from the sales/VAT base.</summary>
    public decimal TipAmount { get; private set; }
    public bool IsPaid { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    /// <summary>
    /// Order-level payment lifecycle, kept in sync with the <see cref="Payments"/> rollup by
    /// <see cref="RecomputePaymentState"/>. Denormalised so the cash counter can filter/index it.
    /// </summary>
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;

    // Legacy inline single-payment mirror — kept populated from the last captured charge so existing
    // receipts and the older cash-summary queries keep working after the move to multi-payment.
    public PaymentMethod? PaymentMethod { get; private set; }
    public decimal? AmountTendered { get; private set; }
    public decimal? ChangeGiven { get; private set; }

    /// <summary>Optimistic-concurrency token guarding against two cashiers settling the same order.</summary>
    public byte[]? RowVersion { get; private set; }

    /// <summary>Set when this invoice's takings have been imported into the accounts (cash counter import). Null = not yet accounted.</summary>
    public DateTime? AccountedAtUtc { get; private set; }

    /// <summary>Cash round-off applied at the POS: negative floors the total, positive ceils it.</summary>
    public decimal RoundingAdjustment { get; private set; }

    /// <summary>Whether this order's stock has been deducted. Driven by the consumption engine, not the status workflow.</summary>
    public StockSyncStatus StockSyncStatus { get; private set; } = StockSyncStatus.NotApplicable;

    /// <summary>Whether the kitchen ticket has been dispatched. Driven by the KOT dispatcher, not the status workflow.</summary>
    public KotPrintStatus KotPrintStatus { get; private set; } = KotPrintStatus.NotPrinted;

    private readonly List<OrderLine> _lines = new();
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    private readonly List<Payment> _payments = new();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    public decimal Subtotal => _lines.Sum(l => l.LineTotal);
    public decimal GrandTotal => Math.Max(0m, Subtotal - DiscountAmount + TaxAmount + ServiceChargeAmount + TipAmount + RoundingAdjustment);
    public decimal Total => GrandTotal;

    /// <summary>Net captured money: captured charges minus captured refunds.</summary>
    public decimal AmountPaid => _payments.Sum(p => p.SignedAmount);
    /// <summary>Outstanding balance the customer still owes.</summary>
    public decimal BalanceDue => Math.Max(0m, GrandTotal - AmountPaid);

    private Order() { }

    public static Order Create(
        string orderNumber,
        Guid customerId,
        Guid? restaurantTableId,
        OrderType orderType,
        DateTime orderedAtUtc,
        string currency = "Tk",
        string? notes = null,
        OrderChannel channel = OrderChannel.Pos)
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
            Channel = channel,
            OrderedAtUtc = orderedAtUtc,
            Currency = currency.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = OrderStatus.Placed
        };
    }

    public OrderLine AddLine(Guid menuItemId, string code, string name, decimal unitPrice, string currency, int quantity = 1, Guid? variantId = null,
        Guid? stationId = null, string? stationName = null, string? notes = null, int prepMinutes = 0)
    {
        if (menuItemId == Guid.Empty) throw new ArgumentException("Menu item is required.", nameof(menuItemId));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));

        var line = new OrderLine
        {
            OrderId = Id,
            MenuItemId = menuItemId,
            VariantId = variantId,
            Code = code,
            Name = name,
            UnitPriceSnapshot = unitPrice,
            Currency = currency.Trim(),
            Quantity = quantity,
            StationId = stationId,
            StationName = string.IsNullOrWhiteSpace(stationName) ? null : stationName.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            PrepMinutes = prepMinutes < 0 ? 0 : prepMinutes
        };
        _lines.Add(line);
        return line;
    }

    /// <summary>
    /// Adds a line, or increases the quantity of an existing line with the same item + variant.
    /// When increasing, the original <see cref="OrderLine.UnitPriceSnapshot"/> deliberately wins
    /// even if the catalog price changed after the order was placed.
    /// </summary>
    public OrderLine AddOrIncreaseLine(Guid menuItemId, string code, string name, decimal unitPrice, string currency, int quantity = 1, Guid? variantId = null,
        Guid? stationId = null, string? stationName = null, string? notes = null, int prepMinutes = 0)
    {
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));

        var existing = _lines.FirstOrDefault(l => l.MenuItemId == menuItemId && l.VariantId == variantId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
            return existing;
        }

        return AddLine(menuItemId, code, name, unitPrice, currency, quantity, variantId, stationId, stationName, notes, prepMinutes);
    }

    public void SetLineQuantity(Guid lineId, int quantity)
    {
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Order line not found.");
        line.Quantity = quantity;
    }

    public void RemoveLine(Guid lineId)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Order line not found.");
        _lines.Remove(line);
    }

    public void Confirm()
    {
        TransitionTo(OrderStatus.Confirmed, expected: OrderStatus.Placed);
        ConfirmedAtUtc = DateTime.UtcNow;
        // Accept = fire: estimate ready time from the slowest line's prep so the customer gets an ETA.
        var prepMinutes = _lines.Count > 0 ? _lines.Max(l => l.PrepMinutes) : 0;
        EstimatedReadyAtUtc = ConfirmedAtUtc.Value.AddMinutes(prepMinutes);
    }

    public void StartPreparing()
    {
        TransitionTo(OrderStatus.Preparing, expected: OrderStatus.Confirmed);
        PreparingAtUtc = DateTime.UtcNow;
    }

    public void MarkReady()
    {
        TransitionTo(OrderStatus.Ready, expected: OrderStatus.Preparing);
        ReadyAtUtc = DateTime.UtcNow;
    }

    public void MarkServed()
    {
        TransitionTo(OrderStatus.Served, expected: OrderStatus.Ready);
        ServedAtUtc = DateTime.UtcNow;
    }

    public void Complete() => TransitionTo(OrderStatus.Completed, expected: OrderStatus.Served);

    /// <summary>
    /// Kitchen Display convenience: advance a not-yet-started order into Preparing in a single step,
    /// auto-confirming if it is still Placed. Throws if the order is past Confirmed.
    /// </summary>
    public void BeginPreparing()
    {
        if (Status == OrderStatus.Placed) Confirm();
        StartPreparing();
    }

    /// <summary>Flags or clears the kitchen priority on this order.</summary>
    public void SetPriority(bool value)
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Completed)
            throw new InvalidOperationException($"Cannot change priority on a {Status} order.");
        IsPriority = value;
    }

    /// <summary>Updates the internal kitchen note (not customer-visible).</summary>
    public void UpdateKitchenNotes(string? notes) => KitchenNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    /// <summary>Attaches this order to a dining session (the visit it was placed during).</summary>
    public void AttachToSession(Guid diningSessionId)
    {
        if (diningSessionId == Guid.Empty) throw new ArgumentException("Session is required.", nameof(diningSessionId));
        DiningSessionId = diningSessionId;
    }

    /// <summary>Detaches this order from its dining session (used when splitting a session).</summary>
    public void DetachFromSession() => DiningSessionId = null;

    /// <summary>Attribute this order to the staff member who took it (dashboard staff metrics).</summary>
    public void AssignWaiter(Guid? waiterUserId, string? waiterName)
    {
        WaiterUserId = waiterUserId;
        WaiterName = string.IsNullOrWhiteSpace(waiterName) ? null : waiterName.Trim();
    }

    /// <summary>Number of diners at the table (dine-in occupancy). Null when not captured.</summary>
    public void SetGuestCount(int? guestCount)
    {
        if (guestCount is < 0) throw new ArgumentOutOfRangeException(nameof(guestCount));
        GuestCount = guestCount is 0 ? null : guestCount;
    }

    /// <summary>Sets tax and service-charge amounts. Default 0 leaves existing totals unchanged.</summary>
    public void ApplyCharges(decimal taxAmount, decimal serviceChargeAmount)
    {
        if (IsPaid) throw new InvalidOperationException("Cannot change charges on a paid order.");
        if (taxAmount < 0m) throw new ArgumentOutOfRangeException(nameof(taxAmount));
        if (serviceChargeAmount < 0m) throw new ArgumentOutOfRangeException(nameof(serviceChargeAmount));
        TaxAmount = taxAmount;
        ServiceChargeAmount = serviceChargeAmount;
    }

    /// <summary>Sets the gratuity. Excluded from the sales/VAT base but added to the payable.</summary>
    public void SetTip(decimal tipAmount)
    {
        if (IsPaid) throw new InvalidOperationException("Cannot change the tip on a paid order.");
        if (tipAmount < 0m) throw new ArgumentOutOfRangeException(nameof(tipAmount));
        TipAmount = tipAmount;
    }

    /// <summary>
    /// Applies every bill adjustment in one shot (discount + tax + service charge + tip + rounding),
    /// ready for the settlement handler to then add the tender payments in the same transaction.
    /// </summary>
    public void Settle(decimal? discountPercent, decimal? discountAmount, string? discountReason,
        decimal taxAmount, decimal serviceChargeAmount, decimal tipAmount, decimal rounding)
    {
        ApplyDiscount(discountPercent, discountAmount, discountReason);
        ApplyCharges(taxAmount, serviceChargeAmount);
        SetTip(tipAmount);
        ApplyRounding(rounding);
    }

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

    public void UpdateTypeAndTable(OrderType orderType, Guid? restaurantTableId)
    {
        EnsureEditable();
        if (orderType == OrderType.DineIn && restaurantTableId is null)
            throw new ArgumentException("Dine-in orders require a table.", nameof(restaurantTableId));

        OrderType = orderType;
        RestaurantTableId = orderType == OrderType.DineIn ? restaurantTableId : null;
    }

    public void ReassignCustomer(Guid customerId)
    {
        EnsureEditable();
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required.", nameof(customerId));
        CustomerId = customerId;
    }

    private void EnsureEditable()
    {
        if (IsPaid) throw new InvalidOperationException("Cannot modify a paid order.");
        if (Status is OrderStatus.Cancelled or OrderStatus.Completed)
            throw new InvalidOperationException($"Cannot modify a {Status} order.");
    }

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

    public void ApplyRounding(decimal adjustment)
    {
        if (IsPaid) throw new InvalidOperationException("Cannot change rounding on a paid order.");
        if (Status == OrderStatus.Cancelled) throw new InvalidOperationException("Cannot round a cancelled order.");
        if (Math.Abs(adjustment) >= 1m)
            throw new ArgumentOutOfRangeException(nameof(adjustment), "Rounding adjustment must be a fraction of one.");
        RoundingAdjustment = adjustment;
    }

    /// <summary>
    /// Back-compat single full payment (legacy POS / BillDialog path). Pays the entire outstanding
    /// balance in one tender; routes through <see cref="AddPayment"/> so the rollup stays authoritative.
    /// </summary>
    public void RecordPayment(PaymentMethod method, decimal tendered)
    {
        if (IsPaid) throw new InvalidOperationException("Order is already paid.");
        if (Status == OrderStatus.Cancelled) throw new InvalidOperationException("Cannot pay a cancelled order.");
        if (Status == OrderStatus.Completed) throw new InvalidOperationException("Order is already completed.");

        var due = BalanceDue;
        if (tendered < due) throw new InvalidOperationException("Amount tendered is less than the amount due.");

        AddPayment(method, null, due, tendered, null, null);
    }

    /// <summary>
    /// Records one tender against the order (supports partial and split payments — call once per tender).
    /// Cash may overpay (the excess becomes change); non-cash is capped at the balance due.
    /// </summary>
    public Payment AddPayment(PaymentMethod method, PaymentProvider? provider, decimal amount, decimal tendered,
        Guid? cashierUserId, string? cashierName, Guid? cashDrawerSessionId = null, string? reference = null)
    {
        if (Status == OrderStatus.Cancelled) throw new InvalidOperationException("Cannot pay a cancelled order.");
        if (PaymentStatus == PaymentStatus.Refunded) throw new InvalidOperationException("Order has been refunded.");
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");

        var balance = BalanceDue;
        if (balance <= 0m) throw new InvalidOperationException("Order is already fully paid.");

        if (amount > balance)
        {
            if (method == PayMethod.Cash) amount = balance; // overpay → change on this tender
            else throw new InvalidOperationException("Payment exceeds the balance due.");
        }

        var payment = Payment.Capture(Id, method, provider, amount, tendered, cashierUserId, cashierName, cashDrawerSessionId, reference);
        _payments.Add(payment);
        RecomputePaymentState();
        return payment;
    }

    /// <summary>Voids a captured charge (mistaken tender). Manager/Admin gated at the handler.</summary>
    public void VoidPayment(Guid paymentId, string reason)
    {
        var payment = _payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new InvalidOperationException("Payment not found.");
        payment.Void(reason);
        RecomputePaymentState();
    }

    /// <summary>Refunds part or all of a captured charge. Returns the refund payment row.</summary>
    public Payment RefundPayment(Guid originalPaymentId, decimal amount, string reason,
        Guid? cashierUserId, string? cashierName, Guid? cashDrawerSessionId = null)
    {
        var original = _payments.FirstOrDefault(p => p.Id == originalPaymentId && p.Kind == PaymentKind.Charge)
            ?? throw new InvalidOperationException("Original payment not found.");
        if (original.Status == PaymentEntryStatus.Voided) throw new InvalidOperationException("Cannot refund a voided payment.");
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Refund amount must be greater than zero.");

        var alreadyRefunded = _payments
            .Where(p => p.Kind == PaymentKind.Refund && p.Status == PaymentEntryStatus.Captured && p.OriginalPaymentId == originalPaymentId)
            .Sum(p => p.Amount);
        var refundable = original.Amount - alreadyRefunded;
        if (amount > refundable) throw new InvalidOperationException("Refund exceeds the refundable amount of the original payment.");

        var refund = Payment.Refund(Id, originalPaymentId, amount, original.Method, original.Provider,
            cashierUserId, cashierName, cashDrawerSessionId, reason);
        _payments.Add(refund);
        if (amount == refundable) original.MarkRefunded();
        RecomputePaymentState();
        return refund;
    }

    /// <summary>
    /// Single source of truth for payment state: rolls up captured payments into <see cref="PaymentStatus"/>
    /// and mirror-maintains the legacy inline fields so existing receipts/queries keep working.
    /// </summary>
    private void RecomputePaymentState()
    {
        var paid = AmountPaid;
        var grand = GrandTotal;
        var hasCharges = _payments.Any(p => p.Kind == PaymentKind.Charge && p.Status == PaymentEntryStatus.Captured);
        var hasRefund = _payments.Any(p => p.Kind == PaymentKind.Refund && p.Status == PaymentEntryStatus.Captured);

        if (Status == OrderStatus.Cancelled)
            PaymentStatus = PaymentStatus.Cancelled;
        else if (hasRefund && paid <= 0m)
            PaymentStatus = PaymentStatus.Refunded;
        else if (paid <= 0m)
            PaymentStatus = PaymentStatus.Pending;
        else if (paid < grand)
            PaymentStatus = PaymentStatus.PartiallyPaid;
        else
            PaymentStatus = PaymentStatus.Paid;

        IsPaid = PaymentStatus == PaymentStatus.Paid;
        if (IsPaid)
            PaidAtUtc ??= DateTime.UtcNow;
        // Payment is orthogonal to the kitchen status: it must NOT move Status. Completion (Served AND
        // Paid) is orchestrated by the settle/serve command handlers, never by this payment rollup.

        var charges = _payments.Where(p => p.Kind == PaymentKind.Charge && p.Status == PaymentEntryStatus.Captured).ToList();
        if (charges.Count > 0)
        {
            var last = charges.OrderBy(p => p.CreatedAtUtc).Last();
            PaymentMethod = last.Method;
            AmountTendered = charges.Sum(p => p.Tendered);
            ChangeGiven = charges.Sum(p => p.Change);
        }
        else
        {
            PaymentMethod = null;
            AmountTendered = null;
            ChangeGiven = null;
        }
    }

    /// <summary>Marks this paid invoice as imported into the accounts. Idempotency is the caller's job (filter on <see cref="AccountedAtUtc"/>).</summary>
    public void MarkAccounted()
    {
        if (!IsPaid) throw new InvalidOperationException("Cannot account an unpaid order.");
        if (AccountedAtUtc is not null) throw new InvalidOperationException("Order is already accounted.");
        AccountedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Reopens this invoice for re-accounting after a post-import refund/void. Idempotent.</summary>
    public void ClearAccounted() => AccountedAtUtc = null;

    /// <summary>Marks stock deduction as in progress (set before the consumption engine runs).</summary>
    public void MarkStockPending() => StockSyncStatus = StockSyncStatus.Pending;
    /// <summary>Marks stock as successfully deducted for this order.</summary>
    public void MarkStockSynced() => StockSyncStatus = StockSyncStatus.Synced;
    /// <summary>Marks the deduction as failed so the retry worker re-attempts it.</summary>
    public void MarkStockFailed() => StockSyncStatus = StockSyncStatus.Failed;
    /// <summary>Marks a prior deduction as reversed (restored to stock on cancellation).</summary>
    public void MarkStockReversed() => StockSyncStatus = StockSyncStatus.Reversed;

    /// <summary>Marks the kitchen ticket dispatch as in progress (set before the print agent is called).</summary>
    public void MarkKotPending() => KotPrintStatus = KotPrintStatus.Pending;
    /// <summary>Marks the kitchen ticket as acknowledged by the print agent.</summary>
    public void MarkKotPrinted() => KotPrintStatus = KotPrintStatus.Printed;
    /// <summary>Marks the dispatch as failed so the retry worker re-attempts it.</summary>
    public void MarkKotFailed() => KotPrintStatus = KotPrintStatus.Failed;

    private void TransitionTo(OrderStatus next, OrderStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Cannot move from {Status} to {next}; expected current status {expected}.");
        Status = next;
    }
}
