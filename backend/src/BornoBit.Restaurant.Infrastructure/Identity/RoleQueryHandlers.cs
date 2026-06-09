using BornoBit.Restaurant.Application.MenuPermissions;
using BornoBit.Restaurant.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Infrastructure.Identity;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly RoleManager<ApplicationRole> _roles;

    public GetRolesQueryHandler(RoleManager<ApplicationRole> roles) => _roles = roles;

    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var query = _roles.Roles.AsQueryable();
        if (!request.IncludeSuperAdmin)
            query = query.Where(r => r.Name != Roles.SuperAdmin);

        var list = await query
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name!))
            .ToListAsync(cancellationToken);

        return list;
    }
}
