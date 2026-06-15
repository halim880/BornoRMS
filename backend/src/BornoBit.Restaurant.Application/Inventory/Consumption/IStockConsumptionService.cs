using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;

namespace BornoBit.Restaurant.Application.Inventory.Consumption;

/// <summary>
/// The unified stock pipeline: deducts (and reverses) inventory for an order's lines. Stateless — the
/// caller passes its own <see cref="IAppDbContext"/> so the writes share the handler's single
/// <c>SaveChangesAsync</c> transaction. Neither method calls SaveChanges. Both are idempotent per order.
/// </summary>
public interface IStockConsumptionService
{
    /// <summary>
    /// Deducts ingredients (recipe) / direct stock for the order, writing <c>ConsumptionOut</c> ledger
    /// rows and bumping the projection. No-op if already applied. Never throws on shortage (stock may go
    /// negative). Sets <see cref="Order.StockSyncStatus"/> to Synced. Requires <c>order.Lines</c> loaded.
    /// </summary>
    Task ApplyConsumptionAsync(IAppDbContext db, Order order, CancellationToken ct);

    /// <summary>
    /// Reverses a prior deduction (order cancellation): restores quantity and writes <c>AdjustmentIn</c>
    /// rows. Restores only the still-outstanding amount (nets any earlier line-void <c>AdjustmentIn</c>
    /// for this order). No-op if nothing was deducted. Sets <see cref="Order.StockSyncStatus"/> to Reversed.
    /// </summary>
    Task ReverseConsumptionAsync(IAppDbContext db, Order order, CancellationToken ct);

    /// <summary>
    /// Reverses just one line's deduction (item void on a stock-synced order): restores the line's
    /// exploded quantity and writes <c>AdjustmentIn</c> rows referencing the order. Leaves the order's
    /// <see cref="Order.StockSyncStatus"/> unchanged (other lines stay deducted). No-op if the line has
    /// no stock impact. Requires <c>order.Lines</c> loaded.
    /// </summary>
    Task ReverseLineAsync(IAppDbContext db, Order order, OrderLine line, CancellationToken ct);
}
