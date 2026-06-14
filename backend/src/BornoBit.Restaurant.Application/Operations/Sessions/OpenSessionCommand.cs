using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>Opens a dine-in session at a table, attributed to the current waiter.</summary>
public record OpenSessionCommand(Guid TableId, int GuestCount = 0) : IRequest<OpenSessionResult>;

public record OpenSessionResult(Guid SessionId, string SessionNumber);

public class OpenSessionCommandValidator : AbstractValidator<OpenSessionCommand>
{
    public OpenSessionCommandValidator()
    {
        RuleFor(x => x.TableId).NotEmpty();
        RuleFor(x => x.GuestCount).GreaterThanOrEqualTo(0);
    }
}

public class OpenSessionCommandHandler : IRequestHandler<OpenSessionCommand, OpenSessionResult>
{
    private readonly IAppDbContext _db;
    private readonly ISessionNumberGenerator _numbers;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;

    public OpenSessionCommandHandler(IAppDbContext db, ISessionNumberGenerator numbers, TimeProvider timeProvider, ICurrentUser currentUser)
    {
        _db = db;
        _numbers = numbers;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
    }

    public async Task<OpenSessionResult> Handle(OpenSessionCommand request, CancellationToken cancellationToken)
    {
        var tableOk = await _db.RestaurantTables.AnyAsync(t => t.Id == request.TableId && t.IsActive, cancellationToken);
        if (!tableOk) throw new NotFoundException("Table not found.");

        var alreadyOpen = await _db.DiningSessions.AnyAsync(
            s => s.RestaurantTableId == request.TableId
                 && (s.Status == DiningSessionStatus.Open || s.Status == DiningSessionStatus.Billing),
            cancellationToken);
        if (alreadyOpen) throw new ConflictException("This table already has an open session.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sessionNumber = await _numbers.NextAsync(nowUtc, cancellationToken);

        var session = DiningSession.Open(sessionNumber, request.TableId, request.GuestCount, nowUtc,
            _currentUser.UserId, _currentUser.UserName);

        _db.DiningSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return new OpenSessionResult(session.Id, session.SessionNumber);
    }
}
