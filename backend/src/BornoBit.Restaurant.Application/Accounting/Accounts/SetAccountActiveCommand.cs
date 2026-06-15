using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Accounts;

public record SetAccountActiveCommand(Guid Id, bool Active) : IRequest<Unit>;

public class SetAccountActiveCommandHandler : IRequestHandler<SetAccountActiveCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetAccountActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetAccountActiveCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        if (request.Active) account.Activate();
        else account.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
