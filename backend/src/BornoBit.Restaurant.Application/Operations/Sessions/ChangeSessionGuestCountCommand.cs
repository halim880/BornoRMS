using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>Updates the diner count on an open session.</summary>
public record ChangeSessionGuestCountCommand(Guid SessionId, int GuestCount) : IRequest<Unit>;

public class ChangeSessionGuestCountCommandValidator : AbstractValidator<ChangeSessionGuestCountCommand>
{
    public ChangeSessionGuestCountCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.GuestCount).GreaterThanOrEqualTo(0);
    }
}

public class ChangeSessionGuestCountCommandHandler : IRequestHandler<ChangeSessionGuestCountCommand, Unit>
{
    private readonly IAppDbContext _db;
    public ChangeSessionGuestCountCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(ChangeSessionGuestCountCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.DiningSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        try { session.ChangeGuestCount(request.GuestCount); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
