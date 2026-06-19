using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.Common;
using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.FixedAssets;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Domain.Kitchen;
using BornoBit.Restaurant.Domain.Logistics;
using BornoBit.Restaurant.Domain.Menus;
using BornoBit.Restaurant.Domain.Numbering;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Domain.Payroll;
using BornoBit.Restaurant.Domain.Settings;
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
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductOptionGroup> ProductOptionGroups => Set<ProductOptionGroup>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<ComboComponent> ComboComponents => Set<ComboComponent>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerOtp> CustomerOtps => Set<CustomerOtp>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<DiningSession> DiningSessions => Set<DiningSession>();
    public DbSet<CustomerRequest> CustomerRequests => Set<CustomerRequest>();
    public DbSet<TableReservation> TableReservations => Set<TableReservation>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderLineModifier> OrderLineModifiers => Set<OrderLineModifier>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<KitchenStation> KitchenStations => Set<KitchenStation>();

    public DbSet<Rider> Riders => Set<Rider>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppMenu> AppMenus => Set<AppMenu>();
    public DbSet<AppMenuRolePermission> AppMenuRolePermissions => Set<AppMenuRolePermission>();
    public DbSet<NumberingScope> NumberingScopes => Set<NumberingScope>();

    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockProjection> StockProjections => Set<StockProjection>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();

    public DbSet<StoreUnit> StoreUnits => Set<StoreUnit>();
    public DbSet<StoreCategory> StoreCategories => Set<StoreCategory>();
    public DbSet<StoreDepartment> StoreDepartments => Set<StoreDepartment>();
    public DbSet<StoreItem> StoreItems => Set<StoreItem>();
    public DbSet<StoreSupplier> StoreSuppliers => Set<StoreSupplier>();
    public DbSet<StoreGoodsReceipt> StoreGoodsReceipts => Set<StoreGoodsReceipt>();
    public DbSet<StoreGoodsReceiptLine> StoreGoodsReceiptLines => Set<StoreGoodsReceiptLine>();
    public DbSet<StoreIssue> StoreIssues => Set<StoreIssue>();
    public DbSet<StoreIssueLine> StoreIssueLines => Set<StoreIssueLine>();
    public DbSet<StoreStockMovement> StoreStockMovements => Set<StoreStockMovement>();
    public DbSet<StorePayment> StorePayments => Set<StorePayment>();
    public DbSet<StoreRequisition> StoreRequisitions => Set<StoreRequisition>();
    public DbSet<StoreRequisitionLine> StoreRequisitionLines => Set<StoreRequisitionLine>();

    public DbSet<CashAccount> CashAccounts => Set<CashAccount>();
    public DbSet<FinanceCategory> FinanceCategories => Set<FinanceCategory>();
    public DbSet<FinanceTransaction> FinanceTransactions => Set<FinanceTransaction>();
    public DbSet<CashDrawerSession> CashDrawerSessions => Set<CashDrawerSession>();
    public DbSet<FinancialAuditLog> FinancialAuditLogs => Set<FinancialAuditLog>();

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();

    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<DepreciationEntry> DepreciationEntries => Set<DepreciationEntry>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollRunLine> PayrollRunLines => Set<PayrollRunLine>();

    public DbSet<RestaurantBillingSettings> RestaurantBillingSettings => Set<RestaurantBillingSettings>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
