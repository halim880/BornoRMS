using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Application.Ordering.Printing;

/// <summary>
/// Dispatches the kitchen ticket as a best-effort side effect of accepting an order, with the same
/// failure-recovery shape as <see cref="Inventory.Consumption.OrderStockSync"/>: a print failure never
/// blocks (or rolls back) the accept. Call AFTER the accept has been saved. Idempotent — safe to call
/// from every accept path; the <c>KotPrintRetryService</c> re-attempts anything left Failed/Pending.
/// </summary>
public static class OrderKotSync
{
    public static async Task TryDispatchAsync(
        IAppDbContext db, IKitchenTicketSender sender, Order order, ILogger logger, CancellationToken ct)
    {
        // Already done or already in flight (e.g. printed at placement, re-reached on KDS accept) — skip.
        if (order.KotPrintStatus is KotPrintStatus.Printed or KotPrintStatus.Pending)
            return;

        var ctx = (DbContext)db;
        order.MarkKotPending();
        try { await db.SaveChangesAsync(ct); } catch { /* best-effort: worker recovers from Pending */ }

        try
        {
            var acknowledged = await sender.SendAsync(order, ct);
            if (acknowledged) order.MarkKotPrinted();
            else order.MarkKotFailed();
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kitchen ticket dispatch failed for order {OrderId}; flagging for retry.", order.Id);
            ctx.ChangeTracker.Clear();
            var fresh = await db.Orders.FirstOrDefaultAsync(o => o.Id == order.Id, ct);
            if (fresh is not null)
            {
                fresh.MarkKotFailed();
                try { await db.SaveChangesAsync(ct); } catch { /* worker still polls Failed */ }
            }
        }
    }
}
