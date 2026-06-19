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

        // Walk the spec tree to any depth. AppMenu.Id is client-generated (Guid.NewGuid()), so a parent's
        // Id is available to its children before SaveChanges — no intermediate saves needed.
        void SeedLevel(List<MenuSpec> specs, Guid? parentId)
        {
            for (var i = 0; i < specs.Count; i++)
            {
                var entity = Upsert(specs[i], parentId, displayOrder: i, byTitle);
                seeded.Add((entity, specs[i]));
                SeedLevel(specs[i].Children, entity.Id);
            }
        }

        SeedLevel(defaults, null);

        await _db.SaveChangesAsync(cancellationToken);
        await BackfillDefaultPermissionsAsync(seeded, cancellationToken);
        await PruneRemovedAccountsMenusAsync(defaults, cancellationToken);
    }

    // The Accounts group was reshaped (GL pages → basic income/expense pages). The upsert above
    // only adds/updates by title, so the old rows (/accounts/coa, /accounts/journals, …) would
    // linger in the sidebar and 404. Deactivate + soft-delete any /accounts/* menu whose URL is
    // no longer in the defaults.
    private async Task PruneRemovedAccountsMenusAsync(List<MenuSpec> defaults, CancellationToken cancellationToken)
    {
        IEnumerable<MenuSpec> Flatten(IEnumerable<MenuSpec> specs) =>
            specs.SelectMany(s => Flatten(s.Children).Prepend(s));

        var validUrls = Flatten(defaults)
            .Select(spec => spec.Url)
            .Where(url => !string.IsNullOrEmpty(url))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stale = await _db.AppMenus
            .IgnoreQueryFilters()
            .Where(m => m.Url != null && m.Url.StartsWith("/accounts/") && !m.IsDeleted)
            .ToListAsync(cancellationToken);

        var pruned = 0;
        foreach (var menu in stale.Where(m => !validUrls.Contains(m.Url!)))
        {
            menu.IsActive = false;
            menu.IsDeleted = true;
            pruned++;
        }

        if (pruned > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AppMenuSeeder: pruned {Count} removed Accounts menu(s).", pruned);
        }
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
            new("Dashboard", Url: "/operations/dashboard", Icon: "DataPie", RequiredRole: Roles.Manager),
            new("Take Order", Url: "/waiter/orders", Icon: "DocumentAdd"),
            new("POS", Url: "/pos", Icon: "ReceiptMoney"),
            new("Cash Counter", Url: "/operations/cash-counter", Icon: "CalculatorMultiple"),
            new("Orders", Url: "/orders", Icon: "DocumentBulletList"),
            new("Print Queue", Url: "/operations/print-queue", Icon: "ReceiptMoney"),
            new("Kitchen Display", Url: "/operations/kitchen-display", Icon: "ClipboardTaskListLtr", RequiredRole: Roles.Chef),
            new("Reports", Icon: "ChartMultiple", RequiredRole: Roles.Manager, Children: new()
            {
                new("Sales Report",       Url: "/operations/reports/sales",      Icon: "ChartMultiple",      RequiredRole: Roles.Manager),
                new("Sales (Invoice-wise)", Url: "/operations/reports/sales-invoices", Icon: "ReceiptMoney",   RequiredRole: Roles.Manager),
                new("Collection Report",  Url: "/operations/reports/collection", Icon: "ReceiptMoney",       RequiredRole: Roles.Manager),
                new("Most Selling Items", Url: "/operations/reports/top-items",  Icon: "ArrowTrendingLines", RequiredRole: Roles.Manager),
                new("Category Sales",     Url: "/operations/reports/category-sales", Icon: "DataPie",        RequiredRole: Roles.Manager),
                new("Cashier Report",     Url: "/operations/reports/cashier",    Icon: "Person",             RequiredRole: Roles.Manager),
                new("Purchase Report",    Url: "/operations/reports/purchases",  Icon: "DocumentBulletList", RequiredRole: Roles.Manager),
                new("Stock Valuation",    Url: "/operations/reports/stock-valuation", Icon: "Box",           RequiredRole: Roles.Manager),
            }),
            new("Menu", Url: "/operations/menu", Icon: "Receipt"),
        }),
        new("System", Icon: "Settings", Children: new()
        {
            new("App Settings", Url: "/settings/app", Icon: "PaintBrush"),
            new("User Manual",  Url: "/system/user-manual", Icon: "BookOpenMicroscope"),
        }),
        new("Delivery", Icon: "VehicleTruck", RequiredRole: Roles.Manager, Children: new()
        {
            new("Dispatch Board", Url: "/logistics/dispatch", Icon: "VehicleTruck", RequiredRole: Roles.Cashier),
            new("COD Reconciliation", Url: "/logistics/cod", Icon: "ReceiptMoney", RequiredRole: Roles.Cashier),
            new("Riders", Url: "/logistics/riders", Icon: "PeopleTeam", RequiredRole: Roles.Manager),
        }),
        new("Catalog", Icon: "BoxMultiple", RequiredRole: Roles.Admin, Children: new()
        {
            new("Products", Url: "/inventory/products", Icon: "Box", RequiredRole: Roles.Admin),
            new("Product Categories", Url: "/inventory/categories", Icon: "FolderList", RequiredRole: Roles.Admin),
            new("Tables", Url: "/inventory/tables", Icon: "Table", RequiredRole: Roles.Admin),
        }),
        new("Inventory", Icon: "Box", RequiredRole: Roles.Manager, Children: new()
        {
            new("Stock Dashboard", Url: "/stock/dashboard", Icon: "DataPie", RequiredRole: Roles.Manager),
            new("Low Stock", Url: "/stock/low", Icon: "Alert", RequiredRole: Roles.Manager),
            new("Stock Items", Url: "/stock/items", Icon: "BoxMultiple", RequiredRole: Roles.Manager),
            new("Product SKUs", Url: "/stock/skus", Icon: "Tag", RequiredRole: Roles.Manager),
            new("Recipes", Url: "/stock/recipes", Icon: "BookOpenMicroscope", RequiredRole: Roles.Manager),
            new("Purchase Orders", Url: "/stock/po", Icon: "DocumentBulletList", RequiredRole: Roles.Manager),
            new("Goods Receipts", Url: "/stock/grn", Icon: "ReceiptMoney", RequiredRole: Roles.Manager),
            new("Wastage", Url: "/stock/wastage", Icon: "Delete", RequiredRole: Roles.Manager),
            new("Stock History", Url: "/stock/history", Icon: "History", RequiredRole: Roles.Manager),
            new("Suppliers", Url: "/stock/suppliers", Icon: "PeopleTeam", RequiredRole: Roles.Admin),
        }),
        new("Store", Icon: "Building", RequiredRole: Roles.Manager, Children: new()
        {
            new("Store Dashboard", Url: "/store/dashboard", Icon: "DataPie", RequiredRole: Roles.Manager),
            new("Store Items", Url: "/store/items", Icon: "BoxMultiple", RequiredRole: Roles.Manager),
            new("Store Categories", Url: "/store/categories", Icon: "FolderList", RequiredRole: Roles.Manager),
            new("Store Departments", Url: "/store/departments", Icon: "PeopleTeam", RequiredRole: Roles.Manager),
            new("Store Receipts", Url: "/store/grn", Icon: "ReceiptMoney", RequiredRole: Roles.Manager),
            new("Store Requisitions", Url: "/store/requisitions", Icon: "ClipboardTask", RequiredRole: Roles.Manager),
            new("Store Issues", Url: "/store/issues", Icon: "DocumentBulletList", RequiredRole: Roles.Manager),
            new("Department Issues", Url: "/store/reports/department-issues", Icon: "ChartMultiple", RequiredRole: Roles.Manager),
            new("Store Ledger", Url: "/store/ledger", Icon: "History", RequiredRole: Roles.Manager),
            new("Store Suppliers", Url: "/store/suppliers", Icon: "PeopleTeam", RequiredRole: Roles.Admin),
            new("Store Payables", Url: "/store/payables", Icon: "ReceiptMoney", RequiredRole: Roles.Admin),
        }),
        new("Accounts", Icon: "Money", RequiredRole: Roles.Admin, Children: new()
        {
            new("Transactions",  Url: "/accounts/transactions",  Icon: "ReceiptMoney", RequiredRole: Roles.Admin),
            new("Supplier Payables", Url: "/accounts/payables",  Icon: "PeopleTeam",   RequiredRole: Roles.Admin),
            new("Cash Accounts", Url: "/accounts/cash-accounts",  Icon: "Wallet",       RequiredRole: Roles.Admin),
            new("Categories",    Url: "/accounts/categories",     Icon: "FolderList",   RequiredRole: Roles.Admin),
            new("Period Close",  Url: "/accounts/periods",        Icon: "CalculatorMultiple", RequiredRole: Roles.Admin),
            new("Fixed Assets",  Url: "/accounts/fixed-assets",   Icon: "Building",     RequiredRole: Roles.Admin),
            new("Bank Reconciliation", Url: "/accounts/bank-rec",  Icon: "Wallet",      RequiredRole: Roles.Admin),
            new("Payroll", Icon: "PeopleTeam", RequiredRole: Roles.Admin, Children: new()
            {
                new("Employees",    Url: "/accounts/payroll/employees", Icon: "Person",            RequiredRole: Roles.Admin),
                new("Payroll Runs", Url: "/accounts/payroll/runs",      Icon: "DocumentBulletList", RequiredRole: Roles.Admin),
            }),
            new("Reporting", Icon: "ChartMultiple", RequiredRole: Roles.Admin, Children: new()
            {
                new("Profit & Loss",  Url: "/accounts/reports/profit-loss", Icon: "ChartMultiple",      RequiredRole: Roles.Admin),
                new("Day-End Close",  Url: "/accounts/reports/day-end",     Icon: "CalculatorMultiple", RequiredRole: Roles.Admin),
                new("Food Cost",      Url: "/accounts/reports/food-cost",   Icon: "ChartMultiple",      RequiredRole: Roles.Manager),
                new("VAT Report",     Url: "/accounts/reports/vat",         Icon: "ReceiptMoney",       RequiredRole: Roles.Admin),
                new("VAT Remittance", Url: "/accounts/reports/vat-remittance", Icon: "ReceiptMoney",    RequiredRole: Roles.Admin),
                new("Cash Book",      Url: "/accounts/reports/cash-book",   Icon: "BookOpenMicroscope", RequiredRole: Roles.Admin),
                new("Account Ledger", Url: "/accounts/reports/ledger",      Icon: "DocumentTable",      RequiredRole: Roles.Admin),
            }),
            new("General Ledger", Icon: "BookOpenMicroscope", RequiredRole: Roles.Admin, Children: new()
            {
                new("Chart of Accounts", Url: "/accounts/gl/chart",         Icon: "AppsList",      RequiredRole: Roles.Admin),
                new("Journal",           Url: "/accounts/gl/journal",       Icon: "DocumentBulletList", RequiredRole: Roles.Admin),
                new("Trial Balance",     Url: "/accounts/gl/trial-balance", Icon: "DocumentTable", RequiredRole: Roles.Admin),
                new("Profit & Loss (GL)",Url: "/accounts/gl/profit-loss",   Icon: "ChartMultiple", RequiredRole: Roles.Admin),
                new("Balance Sheet",     Url: "/accounts/gl/balance-sheet", Icon: "DataPie",       RequiredRole: Roles.Admin),
            }),
        }),
        new("Administration", Icon: "Settings", RequiredRole: Roles.Admin, Children: new()
        {
            new("Users & Roles", Url: "/admin/users", Icon: "PeopleTeam", RequiredRole: Roles.Admin),
            // Note: BackfillDefaultPermissionsAsync re-adds the default Admin grant for these
            // entries on every boot, even if an admin removed it on the mapping pages.
            new("Roles", Url: "/admin/roles", Icon: "Person", RequiredRole: Roles.Admin),
            new("Menu Permissions", Url: "/admin/menu-permissions", Icon: "ShieldKeyhole", RequiredRole: Roles.Admin),
            new("Module Permissions", Url: "/admin/module-permissions", Icon: "AppsList", RequiredRole: Roles.Admin),
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
