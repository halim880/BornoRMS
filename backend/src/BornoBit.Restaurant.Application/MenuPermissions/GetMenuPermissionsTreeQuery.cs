using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.MenuPermissions;

public record MenuPermissionNodeDto(
    Guid Id,
    string Title,
    string? Url,
    string? Icon,
    int DisplayOrder,
    bool IsPermitted,
    IReadOnlyList<MenuPermissionNodeDto> Children
);

public record GetMenuPermissionsTreeQuery(Guid RoleId) : IRequest<IReadOnlyList<MenuPermissionNodeDto>>;

public class GetMenuPermissionsTreeQueryHandler
    : IRequestHandler<GetMenuPermissionsTreeQuery, IReadOnlyList<MenuPermissionNodeDto>>
{
    private readonly IAppDbContext _db;

    public GetMenuPermissionsTreeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MenuPermissionNodeDto>> Handle(
        GetMenuPermissionsTreeQuery request, CancellationToken cancellationToken)
    {
        var menus = await _db.AppMenus
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Title)
            .Select(m => new { m.Id, m.Title, m.Url, m.Icon, m.ParentId, m.DisplayOrder })
            .ToListAsync(cancellationToken);

        var permittedIds = (await _db.AppMenuRolePermissions
            .Where(p => p.RoleId == request.RoleId)
            .Select(p => p.MenuId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var byParent = menus.ToLookup(m => m.ParentId);

        IReadOnlyList<MenuPermissionNodeDto> Build(Guid? parentId) =>
            byParent[parentId]
                .Select(m => new MenuPermissionNodeDto(
                    m.Id, m.Title, m.Url, m.Icon, m.DisplayOrder,
                    permittedIds.Contains(m.Id), Build(m.Id)))
                .ToList();

        return Build(null);
    }
}
