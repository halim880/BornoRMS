using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Dining.Commands;

/// <summary>How long a table edit-hold lasts before it auto-expires (conservative; tune in one place).</summary>
public static class TableHoldOptions
{
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(3);
}

/// <summary>
/// Acquire a short-lived hold on a dine-in table when a terminal starts an order for it, so a second
/// terminal cannot grab the same table mid-edit. Idempotent for the same user (refreshes the timeout);
/// throws <see cref="ConflictException"/> if another terminal already holds it.
/// </summary>
public record AcquireTableHoldCommand(Guid TableId) : IRequest<Unit>;

public class AcquireTableHoldCommandHandler : IRequestHandler<AcquireTableHoldCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AcquireTableHoldCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(AcquireTableHoldCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            throw new ForbiddenException("You must be signed in to hold a table.");

        var table = await _db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == request.TableId, cancellationToken)
            ?? throw new NotFoundException("Table not found.");

        try
        {
            table.Hold(userId, _currentUser.UserName, DateTime.UtcNow, TableHoldOptions.Duration);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("That table was just taken by another terminal. Pick another.");
        }

        return Unit.Value;
    }
}

/// <summary>Release a table hold on close/cancel. No-op if the hold has already moved to another terminal.</summary>
public record ReleaseTableHoldCommand(Guid TableId) : IRequest<Unit>;

public class ReleaseTableHoldCommandHandler : IRequestHandler<ReleaseTableHoldCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ReleaseTableHoldCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ReleaseTableHoldCommand request, CancellationToken cancellationToken)
    {
        var table = await _db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == request.TableId, cancellationToken);
        if (table is null) return Unit.Value;

        table.ReleaseHold(_currentUser.UserId, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
