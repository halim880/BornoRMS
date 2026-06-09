using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Numbering;

public record SetNumberingScopeActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetNumberingScopeActiveCommandHandler : IRequestHandler<SetNumberingScopeActiveCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetNumberingScopeActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetNumberingScopeActiveCommand request, CancellationToken cancellationToken)
    {
        var scope = await _db.NumberingScopes.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Numbering scope {request.Id} not found.");

        if (request.IsActive) scope.Activate();
        else scope.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
