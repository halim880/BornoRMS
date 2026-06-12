using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.MenuPermissions;

public record ModulePermissionDto(
    Guid Id,
    string Title,
    string? Icon,
    int DisplayOrder,
    bool IsPermitted,
    int PermittedChildCount
);

public record GetModulePermissionsQuery(Guid RoleId) : IRequest<IReadOnlyList<ModulePermissionDto>>;

public class GetModulePermissionsQueryHandler
    : IRequestHandler<GetModulePermissionsQuery, IReadOnlyList<ModulePermissionDto>>
{
    private readonly IAppDbContext _db;

    public GetModulePermissionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ModulePermissionDto>> Handle(
        GetModulePermissionsQuery request, CancellationToken cancellationToken)
    {
        var menus = await _db.AppMenus
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Title)
            .Select(m => new { m.Id, m.Title, m.Icon, m.ParentId, m.DisplayOrder })
            .ToListAsync(cancellationToken);

        var permittedIds = (await _db.AppMenuRolePermissions
            .Where(p => p.RoleId == request.RoleId)
            .Select(p => p.MenuId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var byParent = menus.ToLookup(m => m.ParentId);

        int CountPermittedDescendants(Guid parentId)
        {
            var count = 0;
            foreach (var child in byParent[parentId])
            {
                if (permittedIds.Contains(child.Id)) count++;
                count += CountPermittedDescendants(child.Id);
            }
            return count;
        }

        return byParent[null]
            .Select(m => new ModulePermissionDto(
                m.Id, m.Title, m.Icon, m.DisplayOrder,
                permittedIds.Contains(m.Id),
                CountPermittedDescendants(m.Id)))
            .ToList();
    }
}
