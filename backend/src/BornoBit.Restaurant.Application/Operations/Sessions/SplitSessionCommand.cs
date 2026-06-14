using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>
/// Splits selected orders off a session onto a new session at a free table (e.g. one party wants a
/// separate bill / table). The chosen orders are re-parented and moved to the target table.
/// </summary>
public record SplitSessionCommand(
    Guid SourceSessionId,
    IReadOnlyList<Guid> OrderIds,
    Guid TargetTableId,
    int GuestCount = 0) : IRequest<OpenSessionResult>;

public class SplitSessionCommandValidator : AbstractValidator<SplitSessionCommand>
{
    public SplitSessionCommandValidator()
    {
        RuleFor(x => x.SourceSessionId).NotEmpty();
        RuleFor(x => x.OrderIds).NotEmpty();
        RuleFor(x => x.TargetTableId).NotEmpty();
        RuleFor(x => x.GuestCount).GreaterThanOrEqualTo(0);
    }
}

public class SplitSessionCommandHandler : IRequestHandler<SplitSessionCommand, OpenSessionResult>
{
    private readonly IAppDbContext _db;
    private readonly ISessionNumberGenerator _numbers;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;

    public SplitSessionCommandHandler(IAppDbContext db, ISessionNumberGenerator numbers, TimeProvider timeProvider, ICurrentUser currentUser)
    {
        _db = db;
        _numbers = numbers;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
    }

    public async Task<OpenSessionResult> Handle(SplitSessionCommand request, CancellationToken cancellationToken)
    {
        var source = await _db.DiningSessions.FirstOrDefaultAsync(s => s.Id == request.SourceSessionId, cancellationToken)
            ?? throw new NotFoundException("Source session not found.");
        if (source.Status is DiningSessionStatus.Closed or DiningSessionStatus.Merged)
            throw new ConflictException("The source session is no longer active.");

        if (request.TargetTableId == source.RestaurantTableId)
            throw new ConflictException("Pick a different table to split onto.");

        var tableOk = await _db.RestaurantTables.AnyAsync(t => t.Id == request.TargetTableId && t.IsActive, cancellationToken);
        if (!tableOk) throw new NotFoundException("Target table not found.");

        var occupied = await _db.DiningSessions.AnyAsync(
            s => s.RestaurantTableId == request.TargetTableId
                 && (s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing),
            cancellationToken);
        if (occupied) throw new ConflictException("The target table already has an open session.");

        var orders = await _db.Orders
            .Where(o => o.DiningSessionId == source.Id && request.OrderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);
        if (orders.Count == 0) throw new NotFoundException("None of the selected orders belong to this session.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sessionNumber = await _numbers.NextAsync(nowUtc, cancellationToken);
        var newSession = DiningSession.Open(sessionNumber, request.TargetTableId, request.GuestCount, nowUtc,
            source.WaiterUserId ?? _currentUser.UserId, source.WaiterName ?? _currentUser.UserName);
        _db.DiningSessions.Add(newSession);

        foreach (var order in orders)
        {
            order.AttachToSession(newSession.Id);
            if (!order.IsPaid && order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Completed)
                order.UpdateTypeAndTable(OrderType.DineIn, request.TargetTableId);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new OpenSessionResult(newSession.Id, newSession.SessionNumber);
    }
}
