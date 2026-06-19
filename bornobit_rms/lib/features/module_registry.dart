import 'package:flutter/widgets.dart';

import '../core/providers/providers.dart'
    show dashboardRoute, ordersRoute, posRoute, waiterRoute;

// Base modules
import 'dashboard/dashboard_screen.dart';
import 'orders/orders_screen.dart';
import 'pos/pos_screen.dart';
import 'waiter/waiter_screen.dart';

// Wave 1 — kitchen, catalog, reports
import 'kitchen/kitchen_display_screen.dart';
import 'catalog/products_screen.dart';
import 'catalog/product_categories_screen.dart';
import 'catalog/tables_screen.dart';
import 'reports/sales_report_screen.dart';
import 'reports/sales_invoice_report_screen.dart';
import 'reports/collection_report_screen.dart';
import 'reports/top_items_report_screen.dart';
import 'reports/category_sales_report_screen.dart';
import 'reports/cashier_report_screen.dart';
import 'reports/purchase_report_screen.dart';
import 'reports/stock_valuation_report_screen.dart';
import 'printing/print_queue_screen.dart';

// Wave 4 — delivery / logistics
import 'delivery/dispatch_board_screen.dart';
import 'delivery/cod_reconciliation_screen.dart';
import 'delivery/riders_screen.dart';

// Wave 2 — stock
import 'stock/stock_dashboard_screen.dart';
import 'stock/stock_items_screen.dart';
import 'stock/skus_screen.dart';
import 'stock/low_stock_screen.dart';
import 'stock/recipes_screen.dart';
import 'stock/suppliers_screen.dart';
import 'stock/purchase_orders_screen.dart';
import 'stock/goods_receipts_screen.dart';
import 'stock/wastage_screen.dart';
import 'stock/stock_movements_screen.dart';

// Wave 2 — accounts / finance / GL
import 'accounts/transactions_screen.dart';
import 'accounts/cash_accounts_screen.dart';
import 'accounts/categories_screen.dart';
import 'accounts/payables_screen.dart';
import 'accounts/periods_screen.dart';
import 'accounts/fixed_assets_screen.dart';
import 'accounts/bank_rec_screen.dart';
import 'accounts/employees_screen.dart';
import 'accounts/payroll_runs_screen.dart';
import 'accounts/profit_loss_screen.dart';
import 'accounts/day_end_screen.dart';
import 'accounts/food_cost_screen.dart';
import 'accounts/vat_report_screen.dart';
import 'accounts/vat_remittance_screen.dart';
import 'accounts/cash_book_screen.dart';
import 'accounts/account_ledger_screen.dart';
import 'accounts/gl/chart_of_accounts_screen.dart';
import 'accounts/gl/journals_screen.dart';
import 'accounts/gl/trial_balance_screen.dart';
import 'accounts/gl/gl_profit_loss_screen.dart';
import 'accounts/gl/balance_sheet_screen.dart';

// Wave 2 — store
import 'store/store_dashboard_screen.dart';
import 'store/store_items_screen.dart';
import 'store/store_categories_screen.dart';
import 'store/store_departments_screen.dart';
import 'store/store_suppliers_screen.dart';
import 'store/store_goods_receipts_screen.dart';
import 'store/store_requisitions_screen.dart';
import 'store/store_issues_screen.dart';
import 'store/store_ledger_screen.dart';
import 'store/store_payables_screen.dart';
import 'store/store_department_issues_screen.dart';

// Wave 3 — admin
import 'admin/users_screen.dart';
import 'admin/roles_screen.dart';
import 'admin/menu_permissions_screen.dart';
import 'admin/module_permissions_screen.dart';
import 'admin/numbering_scopes_screen.dart';
import 'admin/tenants_screen.dart';
import 'admin/modules_screen.dart';

// Wave 3 — settings
import 'settings/app_settings_screen.dart';
import 'settings/user_manual_screen.dart';

/// Central map of DB-menu route URL -> screen builder. The shell looks a module
/// up here; anything missing falls through to the "not built yet" placeholder.
/// Each ported feature owns its route constant (declared in its screen file).
final Map<String, WidgetBuilder> moduleRoutes = {
  // base
  dashboardRoute: (_) => const DashboardScreen(),
  ordersRoute: (_) => const OrdersScreen(),
  posRoute: (_) => const PosScreen(),
  waiterRoute: (_) => const WaiterScreen(),
  // kitchen / catalog / reports
  kitchenDisplayRoute: (_) => const KitchenDisplayScreen(),
  productsRoute: (_) => const ProductsScreen(),
  productCategoriesRoute: (_) => const ProductCategoriesScreen(),
  tablesAdminRoute: (_) => const TablesScreen(),
  salesReportRoute: (_) => const SalesReportScreen(),
  salesInvoiceReportRoute: (_) => const SalesInvoiceReportScreen(),
  collectionReportRoute: (_) => const CollectionReportScreen(),
  topItemsReportRoute: (_) => const TopItemsReportScreen(),
  categorySalesReportRoute: (_) => const CategorySalesReportScreen(),
  cashierReportRoute: (_) => const CashierReportScreen(),
  purchaseReportRoute: (_) => const PurchaseReportScreen(),
  stockValuationReportRoute: (_) => const StockValuationReportScreen(),
  printQueueRoute: (_) => const PrintQueueScreen(),
  // delivery / logistics
  dispatchBoardRoute: (_) => const DispatchBoardScreen(),
  codReconciliationRoute: (_) => const CodReconciliationScreen(),
  ridersRoute: (_) => const RidersScreen(),
  // stock
  stockDashboardRoute: (_) => const StockDashboardScreen(),
  stockItemsRoute: (_) => const StockItemsScreen(),
  skusRoute: (_) => const SkusScreen(),
  lowStockRoute: (_) => const LowStockScreen(),
  recipesRoute: (_) => const RecipesScreen(),
  suppliersRoute: (_) => const SuppliersScreen(),
  purchaseOrdersRoute: (_) => const PurchaseOrdersScreen(),
  goodsReceiptsRoute: (_) => const GoodsReceiptsScreen(),
  wastageRoute: (_) => const WastageScreen(),
  stockMovementsRoute: (_) => const StockMovementsScreen(),
  // accounts
  transactionsRoute: (_) => const TransactionsScreen(),
  cashAccountsRoute: (_) => const CashAccountsScreen(),
  accountCategoriesRoute: (_) => const AccountCategoriesScreen(),
  payablesRoute: (_) => const PayablesScreen(),
  periodsRoute: (_) => const PeriodsScreen(),
  fixedAssetsRoute: (_) => const FixedAssetsScreen(),
  bankRecRoute: (_) => const BankRecScreen(),
  employeesRoute: (_) => const EmployeesScreen(),
  payrollRunsRoute: (_) => const PayrollRunsScreen(),
  profitLossRoute: (_) => const ProfitLossScreen(),
  dayEndRoute: (_) => const DayEndScreen(),
  foodCostRoute: (_) => const FoodCostScreen(),
  vatReportRoute: (_) => const VatReportScreen(),
  vatRemittanceRoute: (_) => const VatRemittanceScreen(),
  cashBookRoute: (_) => const CashBookScreen(),
  accountLedgerRoute: (_) => const AccountLedgerScreen(),
  chartOfAccountsRoute: (_) => const ChartOfAccountsScreen(),
  journalsRoute: (_) => const JournalsScreen(),
  trialBalanceRoute: (_) => const TrialBalanceScreen(),
  glProfitLossRoute: (_) => const GlProfitLossScreen(),
  balanceSheetRoute: (_) => const BalanceSheetScreen(),
  // store
  storeDashboardRoute: (_) => const StoreDashboardScreen(),
  storeItemsRoute: (_) => const StoreItemsScreen(),
  storeCategoriesRoute: (_) => const StoreCategoriesScreen(),
  storeDepartmentsRoute: (_) => const StoreDepartmentsScreen(),
  storeSuppliersRoute: (_) => const StoreSuppliersScreen(),
  storeGoodsReceiptsRoute: (_) => const StoreGoodsReceiptsScreen(),
  storeRequisitionsRoute: (_) => const StoreRequisitionsScreen(),
  storeIssuesRoute: (_) => const StoreIssuesScreen(),
  storeLedgerRoute: (_) => const StoreLedgerScreen(),
  storePayablesRoute: (_) => const StorePayablesScreen(),
  storeDepartmentIssuesRoute: (_) => const StoreDepartmentIssuesScreen(),
  // admin
  usersRoute: (_) => const UsersScreen(),
  rolesAdminRoute: (_) => const RolesAdminScreen(),
  menuPermissionsRoute: (_) => const MenuPermissionsScreen(),
  modulePermissionsRoute: (_) => const ModulePermissionsScreen(),
  numberingScopesRoute: (_) => const NumberingScopesScreen(),
  tenantsRoute: (_) => const TenantsScreen(),
  modulesRoute: (_) => const ModulesScreen(),
  // settings
  appSettingsRoute: (_) => const AppSettingsScreen(),
  userManualRoute: (_) => const UserManualScreen(),
};
