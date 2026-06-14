using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>The kind of financial action recorded in the audit trail.</summary>
public enum FinancialAuditAction
{
    PaymentCaptured = 0,
    PaymentVoided = 1,
    Refunded = 2,
    DiscountApplied = 3,
    ServiceChargeApplied = 4,
    OrderSettled = 5,
    DrawerOpened = 6,
    DrawerClosed = 7,
    CashImported = 8
}

/// <summary>
/// Append-only financial audit entry: who did what, when, to which entity, with before/after snapshots.
/// Never updated or deleted — the immutable accountability trail for every money-moving action.
/// </summary>
public class FinancialAuditLog : BaseEntity
{
    public DateTime TimestampUtc { get; private set; }
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; } = default!;
    public FinancialAuditAction Action { get; private set; }
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string? OrderNumber { get; private set; }
    public decimal? Amount { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string? Notes { get; private set; }

    private FinancialAuditLog() { }

    public static FinancialAuditLog Record(
        FinancialAuditAction action,
        string entityType,
        Guid entityId,
        Guid? userId,
        string? userName,
        decimal? amount = null,
        string? before = null,
        string? after = null,
        string? orderNumber = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));

        return new FinancialAuditLog
        {
            TimestampUtc = DateTime.UtcNow,
            UserId = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "system" : userName.Trim(),
            Action = action,
            EntityType = entityType.Trim(),
            EntityId = entityId,
            OrderNumber = string.IsNullOrWhiteSpace(orderNumber) ? null : orderNumber.Trim(),
            Amount = amount,
            BeforeJson = before,
            AfterJson = after,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }
}
