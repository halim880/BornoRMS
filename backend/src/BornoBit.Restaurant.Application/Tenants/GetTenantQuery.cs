using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Tenants;

public record GetTenantQuery(Guid Id) : IRequest<TenantDto?>;

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantDto?>
{
    private readonly IAppDbContext _db;

    public GetTenantQueryHandler(IAppDbContext db) => _db = db;

    public Task<TenantDto?> Handle(GetTenantQuery request, CancellationToken cancellationToken) =>
        _db.Tenants
            .Where(t => t.Id == request.Id)
            .Select(t => new TenantDto(
                t.Id, t.Name, t.Subdomain, t.ContactEmail,
                t.IsActive, t.LicenseExpiresOnUtc, t.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
}
