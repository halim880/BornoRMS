using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Menus;

public record GetMenuTreeQuery(IReadOnlyList<Guid>? UserRoleIds = null, bool IsSuperAdmin = false)
    : IRequest<IReadOnlyList<MenuItemDto>>;

public class GetMenuTreeQueryHandler : IRequestHandler<GetMenuTreeQuery, IReadOnlyList<MenuItemDto>>
{
    private readonly IAppDbContext _db;

    public GetMenuTreeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MenuItemDto>> Handle(GetMenuTreeQuery request, CancellationToken cancellationToken)
    {
        var flat = await _db.AppMenus
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Title)
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.Url,
                m.Icon,
                m.ParentId,
                m.DisplayOrder,
                m.RequiredRole
            })
            .ToListAsync(cancellationToken);

        HashSet<Guid> permittedMenuIds;
        if (request.IsSuperAdmin)
        {
            permittedMenuIds = flat.Select(m => m.Id).ToHashSet();
        }
        else if (request.UserRoleIds is { Count: > 0 })
        {
            var ids = request.UserRoleIds;
            permittedMenuIds = (await _db.AppMenuRolePermissions
                .Where(p => ids.Contains(p.RoleId))
                .Select(p => p.MenuId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();
        }
        else
        {
            permittedMenuIds = new HashSet<Guid>();
        }

        var byParent = flat.ToLookup(m => m.ParentId);
        var visibleIds = new HashSet<Guid>();
        foreach (var leaf in flat.Where(m => permittedMenuIds.Contains(m.Id)))
        {
            visibleIds.Add(leaf.Id);
            var p = leaf.ParentId;
            while (p is not null && visibleIds.Add(p.Value))
            {
                p = flat.FirstOrDefault(m => m.Id == p)?.ParentId;
            }
        }

        IReadOnlyList<MenuItemDto> Build(Guid? parentId) =>
            byParent[parentId]
                .Where(m => visibleIds.Contains(m.Id))
                .Select(m => new MenuItemDto(m.Id, m.Title, m.Url, m.Icon, m.DisplayOrder, m.RequiredRole, Build(m.Id)))
                .ToList();

        return Build(null);
    }
}
