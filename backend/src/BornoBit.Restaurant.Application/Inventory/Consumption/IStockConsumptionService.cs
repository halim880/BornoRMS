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
    /// rows. No-op if nothing was deducted. Sets <see cref="Order.StockSyncStatus"/> to Reversed.
    /// </summary>
    Task ReverseConsumptionAsync(IAppDbContext db, Order order, CancellationToken ct);
}
