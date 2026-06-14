using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>Closes a dining session. All of its orders must be settled (paid or cancelled) first.</summary>
public record CloseSessionCommand(Guid SessionId, string? Reason = null) : IRequest<Unit>;

public class CloseSessionCommandValidator : AbstractValidator<CloseSessionCommand>
{
    public CloseSessionCommandValidator() => RuleFor(x => x.SessionId).NotEmpty();
}

public class CloseSessionCommandHandler : IRequestHandler<CloseSessionCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CloseSessionCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(CloseSessionCommand request, CancellationToken cancellationToken)
    {
        PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager, Roles.Cashier);

        var session = await _db.DiningSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        var hasUnsettled = await _db.Orders.AnyAsync(
            o => o.DiningSessionId == session.Id && !o.IsPaid && o.Status != OrderStatus.Cancelled,
            cancellationToken);
        if (hasUnsettled) throw new ConflictException("Cannot close: the session has unpaid orders.");

        try
        {
            session.Close(request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
