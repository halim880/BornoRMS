using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Tenants;

public record GetTenantsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<TenantDto>>;

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, IReadOnlyList<TenantDto>>
{
    private readonly IAppDbContext _db;

    public GetTenantsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Tenants.AsQueryable();
        if (!request.IncludeInactive) query = query.Where(t => t.IsActive);

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto(
                t.Id,
                t.Name,
                t.Subdomain,
                t.ContactEmail,
                t.IsActive,
                t.LicenseExpiresOnUtc,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
