using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>Reassigns a session to another waiter (e.g. shift handover).</summary>
public record TransferSessionWaiterCommand(Guid SessionId, Guid? WaiterUserId, string? WaiterName) : IRequest<Unit>;

public class TransferSessionWaiterCommandValidator : AbstractValidator<TransferSessionWaiterCommand>
{
    public TransferSessionWaiterCommandValidator() => RuleFor(x => x.SessionId).NotEmpty();
}

public class TransferSessionWaiterCommandHandler : IRequestHandler<TransferSessionWaiterCommand, Unit>
{
    private readonly IAppDbContext _db;
    public TransferSessionWaiterCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(TransferSessionWaiterCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.DiningSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        try { session.TransferWaiter(request.WaiterUserId, request.WaiterName); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
