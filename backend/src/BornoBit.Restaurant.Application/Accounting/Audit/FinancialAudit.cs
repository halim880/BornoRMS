using System.Text.Json;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Identity;

namespace BornoBit.Restaurant.Application.Accounting.Audit;

/// <summary>
/// Helper for writing the immutable financial audit trail. Handlers capture a <see cref="Snapshot"/>
/// of the order before mutating, mutate, capture another after, then <see cref="Write"/> the entry
/// (added to the same unit of work — committed atomically with the change).
/// </summary>
public static class FinancialAudit
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    /// <summary>Canonical billing snapshot used as the before/after value in the trail.</summary>
    public static string Snapshot(Order o) => JsonSerializer.Serialize(new
    {
        o.Subtotal,
        o.DiscountAmount,
        o.DiscountPercent,
        o.TaxAmount,
        o.ServiceChargeAmount,
        o.TipAmount,
        o.RoundingAdjustment,
        o.GrandTotal,
        o.AmountPaid,
        o.BalanceDue,
        PaymentStatus = o.PaymentStatus.ToString()
    }, Options);

    public static string Snapshot(CashDrawerSession d) => JsonSerializer.Serialize(new
    {
        d.DrawerNumber,
        d.OpeningBalance,
        d.CashReceived,
        d.CashPaidOut,
        d.CountedClosingBalance,
        d.ExpectedClosingBalance,
        d.Variance,
        Status = d.Status.ToString()
    }, Options);

    public static void Write(
        IAppDbContext db,
        FinancialAuditAction action,
        ICurrentUser user,
        string entityType,
        Guid entityId,
        string? orderNumber = null,
        decimal? amount = null,
        string? before = null,
        string? after = null,
        string? notes = null)
    {
        db.FinancialAuditLogs.Add(FinancialAuditLog.Record(
            action, entityType, entityId, user.UserId, user.UserName, amount, before, after, orderNumber, notes));
    }
}
