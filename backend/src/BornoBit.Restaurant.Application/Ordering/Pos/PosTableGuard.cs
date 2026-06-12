using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Pos;

/// <summary>
/// Validates a dine-in table at the POS: it must exist, be active, and not be held by another
/// open (unpaid, not cancelled/completed) order. Occupancy is derived, never stored.
/// </summary>
internal static class PosTableGuard
{
    public static async Task EnsureAvailableAsync(
        IAppDbContext db, Guid tableId, Guid? currentOrderId, CancellationToken cancellationToken)
    {
        var table = await db.RestaurantTables
            .Where(t => t.Id == tableId && t.IsActive)
            .Select(t => new { t.TableNumber })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Table not found.");

        var occupied = await db.Orders.AnyAsync(o =>
            o.RestaurantTableId == tableId
            && o.Id != currentOrderId
            && !o.IsPaid
            && o.Status != OrderStatus.Cancelled
            && o.Status != OrderStatus.Completed, cancellationToken);

        if (occupied)
            throw new ConflictException($"Table {table.TableNumber} is occupied by another open order.");
    }
}
