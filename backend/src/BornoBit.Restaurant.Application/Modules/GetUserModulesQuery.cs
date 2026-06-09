using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Modules;

/// <summary>
/// Returns root-level AppMenu rows the current user has access to (= has permission to at least
/// one descendant, or to the root row itself for empty-children modules). Used by the
/// ModuleSwitcher in the top bar and to compute the default landing URL per module.
/// </summary>
public record GetUserModulesQuery(
    IReadOnlyList<Guid>? UserRoleIds = null,
    bool IsSuperAdmin = false
) : IRequest<IReadOnlyList<ModuleDto>>;

public class GetUserModulesQueryHandler : IRequestHandler<GetUserModulesQuery, IReadOnlyList<ModuleDto>>
{
    private readonly IAppDbContext _db;

    public GetUserModulesQueryHandler(IAppDbContext db) => _db = db;

    private record MenuRow(Guid Id, string Title, string? Url, string? Icon, Guid? ParentId, int DisplayOrder, string? RequiredRole);

    public async Task<IReadOnlyList<ModuleDto>> Handle(GetUserModulesQuery request, CancellationToken cancellationToken)
    {
        var allActiveMenus = await _db.AppMenus
            .Where(m => m.IsActive)
            .Select(m => new MenuRow(m.Id, m.Title, m.Url, m.Icon, m.ParentId, m.DisplayOrder, m.RequiredRole))
            .ToListAsync(cancellationToken);

        HashSet<Guid> permittedMenuIds;
        if (request.IsSuperAdmin)
        {
            permittedMenuIds = allActiveMenus.Select(m => m.Id).ToHashSet();
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

        var byParent = allActiveMenus.ToLookup(m => m.ParentId);
        var roots = byParent[null].OrderBy(m => m.DisplayOrder).ThenBy(m => m.Title).ToList();

        var dtos = new List<ModuleDto>();
        foreach (var root in roots)
        {
            var descendants = CollectDescendants(root.Id, byParent);
            var accessibleDescendants = descendants.Where(d => permittedMenuIds.Contains(d.Id)).ToList();
            var rootAccessible = permittedMenuIds.Contains(root.Id);

            if (!rootAccessible && accessibleDescendants.Count == 0) continue;

            var firstUrl = accessibleDescendants
                .Where(m => !string.IsNullOrWhiteSpace(m.Url))
                .OrderBy(m => m.DisplayOrder)
                .Select(m => m.Url)
                .FirstOrDefault()
                ?? (rootAccessible && !string.IsNullOrWhiteSpace(root.Url) ? root.Url : null);

            var accessibleUrls = accessibleDescendants
                .Where(m => !string.IsNullOrWhiteSpace(m.Url))
                .Select(m => m.Url!)
                .ToList();
            if (rootAccessible && !string.IsNullOrWhiteSpace(root.Url))
                accessibleUrls.Insert(0, root.Url!);

            dtos.Add(new ModuleDto(
                root.Id,
                root.Title,
                root.Icon,
                root.DisplayOrder,
                IsActive: true,
                root.RequiredRole,
                firstUrl,
                accessibleDescendants.Count,
                accessibleUrls));
        }

        return dtos;
    }

    private static List<MenuRow> CollectDescendants(Guid rootId, ILookup<Guid?, MenuRow> byParent)
    {
        var result = new List<MenuRow>();
        var stack = new Stack<Guid>();
        stack.Push(rootId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            foreach (var child in byParent[id])
            {
                result.Add(child);
                stack.Push(child.Id);
            }
        }
        return result;
    }
}
