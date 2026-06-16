using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.FixedAssets;
using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Domain.Kitchen;
using BornoBit.Restaurant.Domain.Menus;
using BornoBit.Restaurant.Domain.Numbering;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Domain.Payroll;
using BornoBit.Restaurant.Domain.Settings;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Common.Persistence;

public interface IAppDbContext
{
    DbSet<MenuCategory> MenuCategories { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductOptionGroup> ProductOptionGroups { get; }
    DbSet<ProductOption> ProductOptions { get; }
    DbSet<ComboComponent> ComboComponents { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeItem> RecipeItems { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerOtp> CustomerOtps { get; }
    DbSet<RestaurantTable> RestaurantTables { get; }
    DbSet<DiningSession> DiningSessions { get; }
    DbSet<CustomerRequest> CustomerRequests { get; }
    DbSet<TableReservation> TableReservations { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderLine> OrderLines { get; }
    DbSet<OrderLineModifier> OrderLineModifiers { get; }
    DbSet<Payment> Payments { get; }
    DbSet<KitchenStation> KitchenStations { get; }

    DbSet<Tenant> Tenants { get; }
    DbSet<AppMenu> AppMenus { get; }
    DbSet<AppMenuRolePermission> AppMenuRolePermissions { get; }
    DbSet<NumberingScope> NumberingScopes { get; }

    DbSet<Unit> Units { get; }
    DbSet<InventoryCategory> InventoryCategories { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<StockProjection> StockProjections { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<GoodsReceiptLine> GoodsReceiptLines { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
    DbSet<SupplierPayment> SupplierPayments { get; }

    // Store / warehouse (isolated from POS Inventory)
    DbSet<StoreUnit> StoreUnits { get; }
    DbSet<StoreCategory> StoreCategories { get; }
    DbSet<StoreDepartment> StoreDepartments { get; }
    DbSet<StoreItem> StoreItems { get; }
    DbSet<StoreSupplier> StoreSuppliers { get; }
    DbSet<StoreGoodsReceipt> StoreGoodsReceipts { get; }
    DbSet<StoreGoodsReceiptLine> StoreGoodsReceiptLines { get; }
    DbSet<StoreIssue> StoreIssues { get; }
    DbSet<StoreIssueLine> StoreIssueLines { get; }
    DbSet<StoreStockMovement> StoreStockMovements { get; }
    DbSet<StorePayment> StorePayments { get; }
    DbSet<StoreRequisition> StoreRequisitions { get; }
    DbSet<StoreRequisitionLine> StoreRequisitionLines { get; }

    DbSet<CashAccount> CashAccounts { get; }
    DbSet<FinanceCategory> FinanceCategories { get; }
    DbSet<FinanceTransaction> FinanceTransactions { get; }
    DbSet<CashDrawerSession> CashDrawerSessions { get; }
    DbSet<FinancialAuditLog> FinancialAuditLogs { get; }

    // General ledger (double-entry, auto-posted from the cash book + optional manual journals)
    DbSet<Account> Accounts { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalLine> JournalLines { get; }
    DbSet<FiscalPeriod> FiscalPeriods { get; }
    DbSet<BankReconciliation> BankReconciliations { get; }

    DbSet<FixedAsset> FixedAssets { get; }
    DbSet<DepreciationEntry> DepreciationEntries { get; }

    DbSet<Employee> Employees { get; }
    DbSet<PayrollRun> PayrollRuns { get; }
    DbSet<PayrollRunLine> PayrollRunLines { get; }

    DbSet<RestaurantBillingSettings> RestaurantBillingSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
