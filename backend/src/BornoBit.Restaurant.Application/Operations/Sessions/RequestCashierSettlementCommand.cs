using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>Flags a session as awaiting cashier settlement (bill requested). Floor shows it as billing.</summary>
public record RequestCashierSettlementCommand(Guid SessionId) : IRequest<Unit>;

public class RequestCashierSettlementCommandValidator : AbstractValidator<RequestCashierSettlementCommand>
{
    public RequestCashierSettlementCommandValidator() => RuleFor(x => x.SessionId).NotEmpty();
}

public class RequestCashierSettlementCommandHandler : IRequestHandler<RequestCashierSettlementCommand, Unit>
{
    private readonly IAppDbContext _db;
    public RequestCashierSettlementCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(RequestCashierSettlementCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.DiningSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        try { session.MarkBilling(); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
