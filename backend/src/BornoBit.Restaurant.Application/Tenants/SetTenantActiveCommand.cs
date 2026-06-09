using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Tenants;

public record SetTenantActiveCommand(Guid Id, bool IsActive) : IRequest;

public class SetTenantActiveCommandHandler : IRequestHandler<SetTenantActiveCommand>
{
    private readonly IAppDbContext _db;

    public SetTenantActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SetTenantActiveCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Tenant {request.Id} not found.");

        if (request.IsActive) tenant.Activate();
        else tenant.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
    }
}
