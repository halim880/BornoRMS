using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Ordering;

/// <summary>Whether this payment row charges the bill or refunds money back.</summary>
public enum PaymentKind
{
    Charge = 0,
    Refund = 1
}

/// <summary>Per-payment state, distinct from the order-level <see cref="PaymentStatus"/>.</summary>
public enum PaymentEntryStatus
{
    Captured = 0,
    Voided = 1,
    Refunded = 2
}

/// <summary>
/// A single money movement against an <see cref="Order"/> — one tender of a split/partial settlement,
/// or a refund. An order can hold many. Created once and only transitions status (void/refund);
/// the amount is immutable. <see cref="Order.RecomputePaymentState"/> rolls these up.
/// </summary>
public class Payment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentProvider? Provider { get; private set; }

    /// <summary>Amount applied to the bill (always positive; sign comes from <see cref="Kind"/>).</summary>
    public decimal Amount { get; private set; }
    /// <summary>Cash handed over for this tender (≥ Amount for cash; equals Amount otherwise).</summary>
    public decimal Tendered { get; private set; }
    /// <summary>Change returned for this tender (Tendered − Amount, never negative).</summary>
    public decimal Change { get; private set; }

    public PaymentKind Kind { get; private set; }
    public PaymentEntryStatus Status { get; private set; } = PaymentEntryStatus.Captured;
    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CashierUserId { get; private set; }
    public string? CashierName { get; private set; }
    public Guid? CashDrawerSessionId { get; private set; }

    /// <summary>For a refund, the charge payment it reverses.</summary>
    public Guid? OriginalPaymentId { get; private set; }

    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public string? VoidReason { get; private set; }

    /// <summary>Client-supplied dedup token for the settle request that created this tender. All tenders of
    /// one split/partial request share the same key; a repeat request with the same key is a no-op upstream.</summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>Signed contribution to the order's amount paid: + for a captured charge, − for a captured refund.</summary>
    public decimal SignedAmount =>
        Status != PaymentEntryStatus.Captured ? 0m : (Kind == PaymentKind.Charge ? Amount : -Amount);

    private Payment() { }

    public static Payment Capture(
        Guid orderId,
        PaymentMethod method,
        PaymentProvider? provider,
        decimal amount,
        decimal tendered,
        Guid? cashierUserId,
        string? cashierName,
        Guid? cashDrawerSessionId = null,
        string? reference = null,
        string? idempotencyKey = null)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order is required.", nameof(orderId));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");

        // Non-cash tenders settle exactly; only cash carries change.
        if (method != PaymentMethod.Cash) tendered = amount;
        if (tendered < amount) throw new InvalidOperationException("Amount tendered is less than the amount due.");

        return new Payment
        {
            OrderId = orderId,
            Method = method,
            Provider = provider == PaymentProvider.None ? null : provider,
            Amount = amount,
            Tendered = tendered,
            Change = Math.Max(0m, tendered - amount),
            Kind = PaymentKind.Charge,
            Status = PaymentEntryStatus.Captured,
            CreatedAtUtc = DateTime.UtcNow,
            CashierUserId = cashierUserId,
            CashierName = string.IsNullOrWhiteSpace(cashierName) ? null : cashierName.Trim(),
            CashDrawerSessionId = cashDrawerSessionId,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim()
        };
    }

    public static Payment Refund(
        Guid orderId,
        Guid originalPaymentId,
        decimal amount,
        PaymentMethod method,
        PaymentProvider? provider,
        Guid? cashierUserId,
        string? cashierName,
        Guid? cashDrawerSessionId,
        string? reason)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order is required.", nameof(orderId));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Refund amount must be greater than zero.");

        return new Payment
        {
            OrderId = orderId,
            Method = method,
            Provider = provider == PaymentProvider.None ? null : provider,
            Amount = amount,
            Tendered = amount,
            Change = 0m,
            Kind = PaymentKind.Refund,
            Status = PaymentEntryStatus.Captured,
            CreatedAtUtc = DateTime.UtcNow,
            CashierUserId = cashierUserId,
            CashierName = string.IsNullOrWhiteSpace(cashierName) ? null : cashierName.Trim(),
            CashDrawerSessionId = cashDrawerSessionId,
            OriginalPaymentId = originalPaymentId,
            VoidReason = null,
            Notes = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        };
    }

    public void Void(string reason)
    {
        if (Status != PaymentEntryStatus.Captured) throw new InvalidOperationException("Only a captured payment can be voided.");
        if (Kind == PaymentKind.Refund) throw new InvalidOperationException("A refund cannot be voided.");
        Status = PaymentEntryStatus.Voided;
        VoidReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>Marks this charge fully refunded (called once the cumulative refunds reach its amount).</summary>
    public void MarkRefunded()
    {
        if (Kind != PaymentKind.Charge) throw new InvalidOperationException("Only a charge can be marked refunded.");
        Status = PaymentEntryStatus.Refunded;
    }

    public void AttachDrawer(Guid cashDrawerSessionId) => CashDrawerSessionId = cashDrawerSessionId;
}
