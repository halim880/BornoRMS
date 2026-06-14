using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Application.Inventory.Consumption;

/// <summary>
/// Drives the consumption engine from an order-status handler with failure recovery + concurrency retry,
/// so a deduction failure never blocks (or rolls back) the status transition. Call AFTER the transition
/// has been saved. Each method owns its own <c>SaveChangesAsync</c>.
/// </summary>
public static class OrderStockSync
{
    private const int MaxAttempts = 3;

    /// <summary>
    /// Marks the order stock-Pending, then deducts with up to 3 retries on optimistic-concurrency
    /// conflict (each retry re-reads fresh quantities). On any other failure the order is flagged
    /// Failed (the retry worker will re-attempt) — never rethrows, so the order stays confirmed.
    /// </summary>
    public static async Task TryApplyAsync(
        IAppDbContext db, IStockConsumptionService consumption, Order order, ILogger logger, CancellationToken ct)
    {
        var ctx = (DbContext)db;
        order.MarkStockPending();
        try { await db.SaveChangesAsync(ct); } catch { /* best-effort: worker recovers from Pending */ }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await consumption.ApplyConsumptionAsync(db, order, ct);
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                ctx.ChangeTracker.Clear();
                order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == order.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stock consumption failed for order {OrderId}; flagging for retry.", order.Id);
                ctx.ChangeTracker.Clear();
                var fresh = await db.Orders.FirstOrDefaultAsync(o => o.Id == order.Id, ct);
                if (fresh is not null)
                {
                    fresh.MarkStockFailed();
                    try { await db.SaveChangesAsync(ct); } catch { /* worker still polls Pending */ }
                }
                return;
            }
        }
    }

    /// <summary>Reverses a prior deduction on cancellation. Logs and swallows failures (rare).</summary>
    public static async Task TryReverseAsync(
        IAppDbContext db, IStockConsumptionService consumption, Order order, ILogger logger, CancellationToken ct)
    {
        try
        {
            await consumption.ReverseConsumptionAsync(db, order, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stock reversal failed for cancelled order {OrderId}.", order.Id);
        }
    }
}
