using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Domain.Menus;
using BornoBit.Restaurant.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class AppMenuSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ILogger<AppMenuSeeder> _logger;

    public AppMenuSeeder(ApplicationDbContext db, RoleManager<ApplicationRole> roles, ILogger<AppMenuSeeder> logger)
    {
        _db = db;
        _roles = roles;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if ((await _db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            _logger.LogWarning("AppMenuSeeder: pending migrations; skipping.");
            return;
        }

        var defaults = BuildDefaults();

        var allMenus = await _db.AppMenus.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var byTitle = allMenus
            .GroupBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var seeded = new List<(AppMenu Entity, MenuSpec Spec)>();

        for (var i = 0; i < defaults.Count; i++)
        {
            var rootSpec = defaults[i];
            var root = Upsert(rootSpec, parentId: null, displayOrder: i, byTitle);
            seeded.Add((root, rootSpec));

            for (var j = 0; j < rootSpec.Children.Count; j++)
            {
                var childSpec = rootSpec.Children[j];
                var child = Upsert(childSpec, parentId: root.Id, displayOrder: j, byTitle);
                seeded.Add((child, childSpec));
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await BackfillDefaultPermissionsAsync(seeded, cancellationToken);
    }

    private AppMenu Upsert(MenuSpec spec, Guid? parentId, int displayOrder, Dictionary<string, List<AppMenu>> byTitle)
    {
        AppMenu? existing = null;
        if (byTitle.TryGetValue(spec.Title, out var matches))
        {
            existing = matches.FirstOrDefault(m => m.ParentId == parentId)
                    ?? matches.FirstOrDefault(m => m.ParentId is null)
                    ?? matches.FirstOrDefault();
        }

        if (existing is null)
        {
            var entity = new AppMenu
            {
                Title = spec.Title,
                Url = spec.Url,
                Icon = spec.Icon,
                ParentId = parentId,
                DisplayOrder = displayOrder,
                IsActive = true,
                RequiredRole = spec.RequiredRole
            };
            _db.AppMenus.Add(entity);
            byTitle.TryAdd(spec.Title, new List<AppMenu>());
            byTitle[spec.Title].Add(entity);
            return entity;
        }

        existing.ParentId = parentId;
        existing.Url = spec.Url;
        existing.Icon = spec.Icon;
        existing.DisplayOrder = displayOrder;
        existing.IsActive = true;
        existing.RequiredRole = spec.RequiredRole;
        existing.IsDeleted = false;
        return existing;
    }

    private async Task BackfillDefaultPermissionsAsync(
        List<(AppMenu Entity, MenuSpec Spec)> seeded, CancellationToken cancellationToken)
    {
        var roleByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in Roles.All)
        {
            var role = await _roles.FindByNameAsync(name);
            if (role is not null) roleByName[name] = role.Id;
        }
        if (roleByName.Count == 0) return;

        var existingGrants = (await _db.AppMenuRolePermissions
            .Select(p => new { p.MenuId, p.RoleId })
            .ToListAsync(cancellationToken))
            .Select(p => (p.MenuId, p.RoleId))
            .ToHashSet();

        var added = 0;
        foreach (var (entity, spec) in seeded)
        {
            foreach (var roleName in DefaultRolesFor(spec))
            {
                if (!roleByName.TryGetValue(roleName, out var roleId)) continue;
                if (!existingGrants.Add((entity.Id, roleId))) continue;
                _db.AppMenuRolePermissions.Add(new AppMenuRolePermission { MenuId = entity.Id, RoleId = roleId });
                added++;
            }
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AppMenuSeeder: backfilled {Count} permission(s).", added);
        }
    }

    // SuperAdmin sees everything implicitly (GetMenuTreeQuery). Grant other roles per spec.
    private static IEnumerable<string> DefaultRolesFor(MenuSpec spec)
    {
        if (string.IsNullOrEmpty(spec.RequiredRole))
            return Roles.All.Where(r => r != Roles.SuperAdmin);

        return spec.RequiredRole switch
        {
            Roles.SuperAdmin => Array.Empty<string>(),
            Roles.Admin => new[] { Roles.Admin },
            _ => new[] { Roles.Admin, spec.RequiredRole! }
        };
    }

    private static List<MenuSpec> BuildDefaults() => new()
    {
        new("Operations", Icon: "ClipboardTextLtr", Children: new()
        {
            new("Take Order", Url: "/waiter/orders", Icon: "DocumentAdd"),
            new("Orders", Url: "/orders", Icon: "DocumentBulletList"),
            new("App Settings", Url: "/settings/app", Icon: "PaintBrush"),
        }),
        new("Administration", Icon: "Settings", RequiredRole: Roles.Admin, Children: new()
        {
            new("Users & Roles", Url: "/admin/users", Icon: "PeopleTeam", RequiredRole: Roles.Admin),
            new("Menu Permissions", Url: "/admin/menu-permissions", Icon: "ShieldKeyhole", RequiredRole: Roles.Admin),
            new("Numbering Scopes", Url: "/admin/numbering-scopes", Icon: "NumberSymbol", RequiredRole: Roles.Admin),
            new("Tenants", Url: "/admin/tenants", Icon: "Building", RequiredRole: Roles.SuperAdmin),
            new("Modules", Url: "/admin/modules", Icon: "AppsList", RequiredRole: Roles.SuperAdmin),
        }),
    };

    private sealed record MenuSpec(
        string Title,
        string? Url = null,
        string? Icon = null,
        string? RequiredRole = null,
        List<MenuSpec>? Children = null)
    {
        public List<MenuSpec> Children { get; init; } = Children ?? new();
    }
}
