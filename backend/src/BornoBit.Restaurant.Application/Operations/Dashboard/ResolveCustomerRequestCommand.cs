using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Marks a customer request resolved, attributing it to the current staff member.</summary>
public record ResolveCustomerRequestCommand(Guid Id) : IRequest<Unit>;

public class ResolveCustomerRequestCommandHandler : IRequestHandler<ResolveCustomerRequestCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ResolveCustomerRequestCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ResolveCustomerRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.CustomerRequests.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Request not found.");

        entity.Resolve(_currentUser.UserName);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
