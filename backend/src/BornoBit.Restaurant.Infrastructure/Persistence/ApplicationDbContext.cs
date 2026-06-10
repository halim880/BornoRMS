using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.Common;
using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Domain.Menus;
using BornoBit.Restaurant.Domain.Numbering;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Domain.Tenants;
using BornoBit.Restaurant.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace BornoBit.Restaurant.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IAppDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerOtp> CustomerOtps => Set<CustomerOtp>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppMenu> AppMenus => Set<AppMenu>();
    public DbSet<AppMenuRolePermission> AppMenuRolePermissions => Set<AppMenuRolePermission>();
    public DbSet<NumberingScope> NumberingScopes => Set<NumberingScope>();

    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();

    public DbSet<StoreUnit> StoreUnits => Set<StoreUnit>();
    public DbSet<StoreCategory> StoreCategories => Set<StoreCategory>();
    public DbSet<StoreItem> StoreItems => Set<StoreItem>();
    public DbSet<StoreSupplier> StoreSuppliers => Set<StoreSupplier>();
    public DbSet<StoreGoodsReceipt> StoreGoodsReceipts => Set<StoreGoodsReceipt>();
    public DbSet<StoreGoodsReceiptLine> StoreGoodsReceiptLines => Set<StoreGoodsReceiptLine>();
    public DbSet<StoreIssue> StoreIssues => Set<StoreIssue>();
    public DbSet<StoreIssueLine> StoreIssueLines => Set<StoreIssueLine>();
    public DbSet<StoreStockMovement> StoreStockMovements => Set<StoreStockMovement>();

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplyGlobalFilters(builder);
    }

    private static void ApplyGlobalFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(ISoftDelete).IsAssignableFrom(clrType)) continue;

            var parameter = Expression.Parameter(clrType, "e");
            var isDeletedProp = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var notDeleted = Expression.Equal(isDeletedProp, Expression.Constant(false));
            var lambda = Expression.Lambda(notDeleted, parameter);
            builder.Entity(clrType).HasQueryFilter(lambda);
        }
    }
}
