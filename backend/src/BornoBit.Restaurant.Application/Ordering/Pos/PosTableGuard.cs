using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Pos;

/// <summary>
/// Validates a dine-in table at the POS: it must exist, be active, and not already be occupied — by an
/// open dining session (the source of truth for the floor) or, as a fallback, an open unpaid order.
/// Occupancy is derived, never stored.
/// </summary>
internal static class PosTableGuard
{
    /// <param name="excludeSessionId">The editing order's own session, ignored so re-saving its own table is allowed.</param>
    public static async Task EnsureAvailableAsync(
        IAppDbContext db, Guid tableId, Guid? currentOrderId, CancellationToken cancellationToken,
        Guid? excludeSessionId = null)
    {
        var table = await db.RestaurantTables
            .Where(t => t.Id == tableId && t.IsActive)
            .Select(t => new { t.TableNumber })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Table not found.");

        // An open/billing session means guests are seated — even if no order exists yet.
        var sessionOpen = await db.DiningSessions.AnyAsync(s =>
            s.RestaurantTableId == tableId
            && (excludeSessionId == null || s.Id != excludeSessionId)
            && (s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing), cancellationToken);

        var occupied = sessionOpen || await db.Orders.AnyAsync(o =>
            o.RestaurantTableId == tableId
            && o.Id != currentOrderId
            && (excludeSessionId == null || o.DiningSessionId != excludeSessionId)
            && !o.IsPaid
            && o.Status != OrderStatus.Cancelled
            && o.Status != OrderStatus.Completed, cancellationToken);

        if (occupied)
            throw new ConflictException($"Table {table.TableNumber} is occupied.");
    }
}
