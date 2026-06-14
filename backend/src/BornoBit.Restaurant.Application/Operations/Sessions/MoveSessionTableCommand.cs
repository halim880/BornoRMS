using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>Moves a session (and its still-open orders) to a different, free table.</summary>
public record MoveSessionTableCommand(Guid SessionId, Guid TargetTableId) : IRequest<Unit>;

public class MoveSessionTableCommandValidator : AbstractValidator<MoveSessionTableCommand>
{
    public MoveSessionTableCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.TargetTableId).NotEmpty();
    }
}

public class MoveSessionTableCommandHandler : IRequestHandler<MoveSessionTableCommand, Unit>
{
    private readonly IAppDbContext _db;
    public MoveSessionTableCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(MoveSessionTableCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.DiningSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        if (request.TargetTableId == session.RestaurantTableId) return Unit.Value;

        var tableOk = await _db.RestaurantTables.AnyAsync(t => t.Id == request.TargetTableId && t.IsActive, cancellationToken);
        if (!tableOk) throw new NotFoundException("Target table not found.");

        var occupied = await _db.DiningSessions.AnyAsync(
            s => s.RestaurantTableId == request.TargetTableId
                 && (s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing),
            cancellationToken);
        if (occupied) throw new ConflictException("The target table already has an open session.");

        try { session.MoveToTable(request.TargetTableId); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw new ConflictException(ex.Message);
        }

        // Keep the open orders' table snapshot in step with the session.
        var orders = await _db.Orders
            .Where(o => o.DiningSessionId == session.Id && !o.IsPaid
                        && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Completed)
            .ToListAsync(cancellationToken);
        foreach (var order in orders)
            order.UpdateTypeAndTable(OrderType.DineIn, request.TargetTableId);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
