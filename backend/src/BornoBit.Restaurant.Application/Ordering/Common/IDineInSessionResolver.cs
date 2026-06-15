using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Identity;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Common;

/// <summary>
/// Single source of truth for dine-in occupancy: maps a table to its open <see cref="DiningSession"/>,
/// opening one when needed, and closes a session once its last active order is gone. Shared by the waiter
/// and POS order flows so every floor view (POS, Waiter, Dashboard) reads the same session model.
/// </summary>
public interface IDineInSessionResolver
{
    /// <summary>
    /// Returns the session a dine-in order on <paramref name="tableId"/> belongs to: the supplied one if still
    /// active, else the table's current open/billing session, else a freshly opened one. A newly opened session
    /// is added to the caller-supplied <paramref name="db"/> so it is persisted by the caller's SaveChanges.
    /// </summary>
    Task<Guid> ResolveAsync(IAppDbContext db, Guid tableId, Guid? requestedSessionId, int? guestCount, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Closes the session when it has no remaining active (unpaid, non-cancelled) orders — frees the table.
    /// No-op if the session is missing, already closed/merged, or still has open orders. Mutates the
    /// caller-supplied <paramref name="db"/> so the close is persisted by the caller's SaveChanges.
    /// </summary>
    Task CloseIfEmptyAsync(IAppDbContext db, Guid sessionId, CancellationToken cancellationToken);
}

public class DineInSessionResolver : IDineInSessionResolver
{
    private readonly ISessionNumberGenerator _sessionNumbers;
    private readonly ICurrentUser _currentUser;

    public DineInSessionResolver(ISessionNumberGenerator sessionNumbers, ICurrentUser currentUser)
    {
        _sessionNumbers = sessionNumbers;
        _currentUser = currentUser;
    }

    public async Task<Guid> ResolveAsync(IAppDbContext db, Guid tableId, Guid? requestedSessionId, int? guestCount, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (requestedSessionId is { } sid)
        {
            var ok = await db.DiningSessions.AnyAsync(
                s => s.Id == sid && s.RestaurantTableId == tableId
                     && (s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing),
                cancellationToken);
            if (ok) return sid;
        }

        var existing = await db.DiningSessions
            .Where(s => s.RestaurantTableId == tableId
                        && (s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing))
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != Guid.Empty) return existing;

        var sessionNumber = await _sessionNumbers.NextAsync(nowUtc, cancellationToken);
        var session = DiningSession.Open(sessionNumber, tableId, guestCount ?? 0, nowUtc,
            _currentUser.UserId, _currentUser.UserName);
        db.DiningSessions.Add(session);
        return session.Id;
    }

    public async Task CloseIfEmptyAsync(IAppDbContext db, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await db.DiningSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null || session.Status is DiningSessionStatus.Closed or DiningSessionStatus.Merged)
            return;

        var hasActiveOrders = await db.Orders.AnyAsync(
            o => o.DiningSessionId == sessionId && !o.IsPaid && o.Status != OrderStatus.Cancelled,
            cancellationToken);
        if (hasActiveOrders) return;

        session.Close("Settled");
    }
}
