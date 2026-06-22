// DTOs for the Accounts / Finance / General-Ledger screens. Field names mirror
// the C# records in BornoBit.Restaurant.Application.Accounting.** (and a couple
// of Inventory.* feature records reused by the payables/food-cost screens),
// serialized camelCase.

// ---- shared parse helpers (copied from the feature convention) ----
double _d(dynamic v) => v == null ? 0.0 : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();
DateTime _dt(dynamic v) => v == null ? DateTime.now() : DateTime.parse(v as String).toLocal();

// ---------------------------------------------------------------------------
// transactions
// ---------------------------------------------------------------------------

/// TransactionListItemDto.
class TransactionRow {
  final String id;
  final String number;
  final DateTime occurredOn;
  final String type; // "Income" | "Expense"
  final String categoryId;
  final String categoryName;
  final String cashAccountId;
  final String cashAccountName;
  final double amount;
  final String? reference;
  final String? notes;

  TransactionRow({
    required this.id,
    required this.number,
    required this.occurredOn,
    required this.type,
    required this.categoryId,
    required this.categoryName,
    required this.cashAccountId,
    required this.cashAccountName,
    required this.amount,
    this.reference,
    this.notes,
  });

  factory TransactionRow.fromJson(Map<String, dynamic> j) => TransactionRow(
        id: _s(j['id']),
        number: _s(j['number']),
        occurredOn: _dt(j['occurredOn']),
        type: _s(j['type']),
        categoryId: _s(j['categoryId']),
        categoryName: _s(j['categoryName']),
        cashAccountId: _s(j['cashAccountId']),
        cashAccountName: _s(j['cashAccountName']),
        amount: _d(j['amount']),
        reference: _sOrNull(j['reference']),
        notes: _sOrNull(j['notes']),
      );
}

/// CategoryTotalDto.
class CategoryTotal {
  final String categoryId;
  final String categoryName;
  final String type;
  final double total;

  CategoryTotal({
    required this.categoryId,
    required this.categoryName,
    required this.type,
    required this.total,
  });

  factory CategoryTotal.fromJson(Map<String, dynamic> j) => CategoryTotal(
        categoryId: _s(j['categoryId']),
        categoryName: _s(j['categoryName']),
        type: _s(j['type']),
        total: _d(j['total']),
      );
}

/// FinanceSummaryDto.
class FinanceSummary {
  final double totalIncome;
  final double totalExpense;
  final double net;
  final List<CategoryTotal> byCategory;

  FinanceSummary({
    required this.totalIncome,
    required this.totalExpense,
    required this.net,
    required this.byCategory,
  });

  factory FinanceSummary.fromJson(Map<String, dynamic> j) => FinanceSummary(
        totalIncome: _d(j['totalIncome']),
        totalExpense: _d(j['totalExpense']),
        net: _d(j['net']),
        byCategory: ((j['byCategory'] as List?) ?? [])
            .map((e) => CategoryTotal.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

// ---------------------------------------------------------------------------
// cash accounts
// ---------------------------------------------------------------------------

/// CashAccountDto.
class CashAccount {
  final String id;
  final String name;
  final String kind; // "Cash" | "MobileWallet" | "Bank"
  final double openingBalance;
  final double balance;
  final bool isActive;

  CashAccount({
    required this.id,
    required this.name,
    required this.kind,
    required this.openingBalance,
    required this.balance,
    required this.isActive,
  });

  factory CashAccount.fromJson(Map<String, dynamic> j) => CashAccount(
        id: _s(j['id']),
        name: _s(j['name']),
        kind: _s(j['kind']),
        openingBalance: _d(j['openingBalance']),
        balance: _d(j['balance']),
        isActive: j['isActive'] == true,
      );
}

// ---------------------------------------------------------------------------
// categories
// ---------------------------------------------------------------------------

/// CategoryDto (finance category).
class FinanceCategory {
  final String id;
  final String name;
  final String type; // "Income" | "Expense"
  final bool isActive;

  FinanceCategory({
    required this.id,
    required this.name,
    required this.type,
    required this.isActive,
  });

  factory FinanceCategory.fromJson(Map<String, dynamic> j) => FinanceCategory(
        id: _s(j['id']),
        name: _s(j['name']),
        type: _s(j['type']),
        isActive: j['isActive'] == true,
      );
}

// ---------------------------------------------------------------------------
// payables
// ---------------------------------------------------------------------------

/// PayableDto.
class PayableRow {
  final String supplierId;
  final String supplierCode;
  final String supplierName;
  final int paymentTermsDays;
  final double received;
  final double returned;
  final double paid;
  final double outstanding;

  PayableRow({
    required this.supplierId,
    required this.supplierCode,
    required this.supplierName,
    required this.paymentTermsDays,
    required this.received,
    required this.returned,
    required this.paid,
    required this.outstanding,
  });

  factory PayableRow.fromJson(Map<String, dynamic> j) => PayableRow(
        supplierId: _s(j['supplierId']),
        supplierCode: _s(j['supplierCode']),
        supplierName: _s(j['supplierName']),
        paymentTermsDays: _i(j['paymentTermsDays']),
        received: _d(j['received']),
        returned: _d(j['returned']),
        paid: _d(j['paid']),
        outstanding: _d(j['outstanding']),
      );
}

/// SupplierPaymentDto.
class SupplierPaymentRow {
  final String id;
  final String supplierId;
  final String supplierName;
  final DateTime paidOn;
  final double amount;
  final String? cashAccountName;
  final String? reference;

  SupplierPaymentRow({
    required this.id,
    required this.supplierId,
    required this.supplierName,
    required this.paidOn,
    required this.amount,
    this.cashAccountName,
    this.reference,
  });

  factory SupplierPaymentRow.fromJson(Map<String, dynamic> j) => SupplierPaymentRow(
        id: _s(j['id']),
        supplierId: _s(j['supplierId']),
        supplierName: _s(j['supplierName']),
        paidOn: _dt(j['paidOn']),
        amount: _d(j['amount']),
        cashAccountName: _sOrNull(j['cashAccountName']),
        reference: _sOrNull(j['reference']),
      );
}

// ---------------------------------------------------------------------------
// fiscal periods
// ---------------------------------------------------------------------------

/// FiscalPeriodDto.
class FiscalPeriod {
  final String id;
  final int year;
  final int month;
  final String name;
  final String status; // "Open" | "Closed"
  final DateTime? closedAtUtc;
  final String? closedBy;

  FiscalPeriod({
    required this.id,
    required this.year,
    required this.month,
    required this.name,
    required this.status,
    this.closedAtUtc,
    this.closedBy,
  });

  factory FiscalPeriod.fromJson(Map<String, dynamic> j) => FiscalPeriod(
        id: _s(j['id']),
        year: _i(j['year']),
        month: _i(j['month']),
        name: _s(j['name']),
        status: _s(j['status']),
        closedAtUtc: j['closedAtUtc'] == null ? null : _dt(j['closedAtUtc']),
        closedBy: _sOrNull(j['closedBy']),
      );
}

// ---------------------------------------------------------------------------
// fixed assets
// ---------------------------------------------------------------------------

/// FixedAssetDto.
class FixedAsset {
  final String id;
  final String assetNumber;
  final String name;
  final String glAccountName;
  final DateTime acquisitionDate;
  final double cost;
  final double salvageValue;
  final int usefulLifeMonths;
  final double accumulatedDepreciation;
  final double netBookValue;
  final String status; // "Active" | "FullyDepreciated" | "Disposed"

  FixedAsset({
    required this.id,
    required this.assetNumber,
    required this.name,
    required this.glAccountName,
    required this.acquisitionDate,
    required this.cost,
    required this.salvageValue,
    required this.usefulLifeMonths,
    required this.accumulatedDepreciation,
    required this.netBookValue,
    required this.status,
  });

  factory FixedAsset.fromJson(Map<String, dynamic> j) => FixedAsset(
        id: _s(j['id']),
        assetNumber: _s(j['assetNumber']),
        name: _s(j['name']),
        glAccountName: _s(j['glAccountName']),
        acquisitionDate: _dt(j['acquisitionDate']),
        cost: _d(j['cost']),
        salvageValue: _d(j['salvageValue']),
        usefulLifeMonths: _i(j['usefulLifeMonths']),
        accumulatedDepreciation: _d(j['accumulatedDepreciation']),
        netBookValue: _d(j['netBookValue']),
        status: _s(j['status']),
      );
}

// ---------------------------------------------------------------------------
// bank reconciliation
// ---------------------------------------------------------------------------

/// BankReconciliationDto.
class BankReconciliation {
  final String id;
  final String cashAccountId;
  final String cashAccountName;
  final DateTime statementDate;
  final double statementBalance;
  final double clearedBalance;
  final String status; // "InProgress" | "Completed"
  final DateTime? completedOn;

  BankReconciliation({
    required this.id,
    required this.cashAccountId,
    required this.cashAccountName,
    required this.statementDate,
    required this.statementBalance,
    required this.clearedBalance,
    required this.status,
    this.completedOn,
  });

  factory BankReconciliation.fromJson(Map<String, dynamic> j) => BankReconciliation(
        id: _s(j['id']),
        cashAccountId: _s(j['cashAccountId']),
        cashAccountName: _s(j['cashAccountName']),
        statementDate: _dt(j['statementDate']),
        statementBalance: _d(j['statementBalance']),
        clearedBalance: _d(j['clearedBalance']),
        status: _s(j['status']),
        completedOn: j['completedOn'] == null ? null : _dt(j['completedOn']),
      );
}

// ---------------------------------------------------------------------------
// payroll
// ---------------------------------------------------------------------------

/// EmployeeDto.
class Employee {
  final String id;
  final String code;
  final String fullName;
  final String? designation;
  final double baseSalary;
  final double overtimeRate;
  final String status; // "Active" | "Inactive"
  final DateTime joinedOn;

  Employee({
    required this.id,
    required this.code,
    required this.fullName,
    this.designation,
    required this.baseSalary,
    required this.overtimeRate,
    required this.status,
    required this.joinedOn,
  });

  factory Employee.fromJson(Map<String, dynamic> j) => Employee(
        id: _s(j['id']),
        code: _s(j['code']),
        fullName: _s(j['fullName']),
        designation: _sOrNull(j['designation']),
        baseSalary: _d(j['baseSalary']),
        overtimeRate: _d(j['overtimeRate']),
        status: _s(j['status']),
        joinedOn: _dt(j['joinedOn']),
      );
}

/// PayrollRunSummaryDto.
class PayrollRunSummary {
  final String id;
  final String runNumber;
  final int year;
  final int month;
  final String status; // "Draft" | "Approved" | "Paid"
  final double totalNet;

  PayrollRunSummary({
    required this.id,
    required this.runNumber,
    required this.year,
    required this.month,
    required this.status,
    required this.totalNet,
  });

  factory PayrollRunSummary.fromJson(Map<String, dynamic> j) => PayrollRunSummary(
        id: _s(j['id']),
        runNumber: _s(j['runNumber']),
        year: _i(j['year']),
        month: _i(j['month']),
        status: _s(j['status']),
        totalNet: _d(j['totalNet']),
      );
}

// ---------------------------------------------------------------------------
// reports — cash-basis P&L
// ---------------------------------------------------------------------------

/// PlLineDto.
class PlLine {
  final String categoryId;
  final String categoryName;
  final double amount;

  PlLine({required this.categoryId, required this.categoryName, required this.amount});

  factory PlLine.fromJson(Map<String, dynamic> j) => PlLine(
        categoryId: _s(j['categoryId']),
        categoryName: _s(j['categoryName']),
        amount: _d(j['amount']),
      );
}

/// ProfitAndLossDto (cash-basis).
class ProfitAndLoss {
  final double totalRevenue;
  final List<PlLine> revenue;
  final double totalCogs;
  final List<PlLine> cogs;
  final double grossProfit;
  final double totalExpenses;
  final List<PlLine> expenses;
  final double netProfit;

  ProfitAndLoss({
    required this.totalRevenue,
    required this.revenue,
    required this.totalCogs,
    required this.cogs,
    required this.grossProfit,
    required this.totalExpenses,
    required this.expenses,
    required this.netProfit,
  });

  static List<PlLine> _lines(dynamic v) =>
      ((v as List?) ?? []).map((e) => PlLine.fromJson(e as Map<String, dynamic>)).toList();

  factory ProfitAndLoss.fromJson(Map<String, dynamic> j) => ProfitAndLoss(
        totalRevenue: _d(j['totalRevenue']),
        revenue: _lines(j['revenue']),
        totalCogs: _d(j['totalCogs']),
        cogs: _lines(j['cogs']),
        grossProfit: _d(j['grossProfit']),
        totalExpenses: _d(j['totalExpenses']),
        expenses: _lines(j['expenses']),
        netProfit: _d(j['netProfit']),
      );
}

// ---------------------------------------------------------------------------
// reports — day end
// ---------------------------------------------------------------------------

/// DrawerMethodLineDto.
class DrawerMethodLine {
  final String method;
  final int count;
  final double amount;

  DrawerMethodLine({required this.method, required this.count, required this.amount});

  factory DrawerMethodLine.fromJson(Map<String, dynamic> j) => DrawerMethodLine(
        method: _s(j['method']),
        count: _i(j['count']),
        amount: _d(j['amount']),
      );
}

/// DrawerDto (subset used in the day-end view).
class DrawerLine {
  final String drawerNumber;
  final String cashierName;
  final String? cashAccountName;
  final String status;
  final double openingBalance;
  final double expectedClosingBalance;
  final double? countedClosingBalance;
  final double variance;

  DrawerLine({
    required this.drawerNumber,
    required this.cashierName,
    this.cashAccountName,
    required this.status,
    required this.openingBalance,
    required this.expectedClosingBalance,
    this.countedClosingBalance,
    required this.variance,
  });

  factory DrawerLine.fromJson(Map<String, dynamic> j) => DrawerLine(
        drawerNumber: _s(j['drawerNumber']),
        cashierName: _s(j['cashierName']),
        cashAccountName: _sOrNull(j['cashAccountName']),
        status: _s(j['status']),
        openingBalance: _d(j['openingBalance']),
        expectedClosingBalance: _d(j['expectedClosingBalance']),
        countedClosingBalance:
            j['countedClosingBalance'] == null ? null : _d(j['countedClosingBalance']),
        variance: _d(j['variance']),
      );
}

/// DayEndReportDto.
/// Result of posting a day's cash-counter takings to the GL.
class CashImportResult {
  final int count;
  final double total;
  final List<String> skippedMethods;
  CashImportResult({required this.count, required this.total, required this.skippedMethods});

  factory CashImportResult.fromJson(Map<String, dynamic> j) => CashImportResult(
        count: _i(j['count']),
        total: _d(j['total']),
        skippedMethods: (j['skippedMethods'] as List? ?? []).map((e) => e.toString()).toList(),
      );
}

class DayEndReport {
  final DateTime date;
  final String currency;
  final int orderCount;
  final double salesSubtotal;
  final double salesDiscount;
  final double salesTotal;
  final List<DrawerMethodLine> byMethod;
  final double totalCollected;
  final List<DrawerLine> drawers;
  final double drawerVariance;
  final List<PlLine> expenses;
  final double totalExpenses;
  final int unaccountedOrders;
  final double unaccountedAmount;

  DayEndReport({
    required this.date,
    required this.currency,
    required this.orderCount,
    required this.salesSubtotal,
    required this.salesDiscount,
    required this.salesTotal,
    required this.byMethod,
    required this.totalCollected,
    required this.drawers,
    required this.drawerVariance,
    required this.expenses,
    required this.totalExpenses,
    required this.unaccountedOrders,
    required this.unaccountedAmount,
  });

  factory DayEndReport.fromJson(Map<String, dynamic> j) => DayEndReport(
        // DateOnly serializes "yyyy-MM-dd"; parse as plain date.
        date: DateTime.parse(_s(j['date'])),
        currency: j['currency'] == null ? 'Tk' : _s(j['currency']),
        orderCount: _i(j['orderCount']),
        salesSubtotal: _d(j['salesSubtotal']),
        salesDiscount: _d(j['salesDiscount']),
        salesTotal: _d(j['salesTotal']),
        byMethod: ((j['byMethod'] as List?) ?? [])
            .map((e) => DrawerMethodLine.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalCollected: _d(j['totalCollected']),
        drawers: ((j['drawers'] as List?) ?? [])
            .map((e) => DrawerLine.fromJson(e as Map<String, dynamic>))
            .toList(),
        drawerVariance: _d(j['drawerVariance']),
        expenses: ((j['expenses'] as List?) ?? [])
            .map((e) => PlLine.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalExpenses: _d(j['totalExpenses']),
        unaccountedOrders: _i(j['unaccountedOrders']),
        unaccountedAmount: _d(j['unaccountedAmount']),
      );
}

/// Sales ↔ GL reconciliation for a business day (GetSalesGlReconciliationQuery).
class SalesGlReconciliation {
  final String currency;
  final double operationalTakings;
  final double postedToBooks;
  final double variance;
  final int unaccountedOrders;
  final double unaccountedAmount;
  final List<String> blockedMethods;
  final bool isReconciled;

  SalesGlReconciliation({
    required this.currency,
    required this.operationalTakings,
    required this.postedToBooks,
    required this.variance,
    required this.unaccountedOrders,
    required this.unaccountedAmount,
    required this.blockedMethods,
    required this.isReconciled,
  });

  factory SalesGlReconciliation.fromJson(Map<String, dynamic> j) => SalesGlReconciliation(
        currency: j['currency'] == null ? 'Tk' : _s(j['currency']),
        operationalTakings: _d(j['operationalTakings']),
        postedToBooks: _d(j['postedToBooks']),
        variance: _d(j['variance']),
        unaccountedOrders: _i(j['unaccountedOrders']),
        unaccountedAmount: _d(j['unaccountedAmount']),
        blockedMethods: (j['blockedMethods'] as List? ?? []).map((e) => e.toString()).toList(),
        isReconciled: j['isReconciled'] as bool? ?? false,
      );
}

// ---------------------------------------------------------------------------
// reports — food cost
// ---------------------------------------------------------------------------

/// FoodCostCategoryRow.
class FoodCostCategory {
  final String category;
  final double cogs;

  FoodCostCategory({required this.category, required this.cogs});

  factory FoodCostCategory.fromJson(Map<String, dynamic> j) =>
      FoodCostCategory(category: _s(j['category']), cogs: _d(j['cogs']));
}

/// FoodCostReportDto.
class FoodCostReport {
  final double netSales;
  final double cogs;
  final double foodCostPercent;
  final double wastage;
  final double inventoryValue;
  final List<FoodCostCategory> categories;
  final String currency;

  FoodCostReport({
    required this.netSales,
    required this.cogs,
    required this.foodCostPercent,
    required this.wastage,
    required this.inventoryValue,
    required this.categories,
    required this.currency,
  });

  factory FoodCostReport.fromJson(Map<String, dynamic> j) => FoodCostReport(
        netSales: _d(j['netSales']),
        cogs: _d(j['cogs']),
        foodCostPercent: _d(j['foodCostPercent']),
        wastage: _d(j['wastage']),
        inventoryValue: _d(j['inventoryValue']),
        categories: ((j['categories'] as List?) ?? [])
            .map((e) => FoodCostCategory.fromJson(e as Map<String, dynamic>))
            .toList(),
        currency: j['currency'] == null ? 'Tk' : _s(j['currency']),
      );
}

// ---------------------------------------------------------------------------
// reports — VAT
// ---------------------------------------------------------------------------

/// VatReportRowDto.
class VatReportRow {
  final double ratePercent;
  final double taxableSales;
  final double vat;

  VatReportRow({required this.ratePercent, required this.taxableSales, required this.vat});

  factory VatReportRow.fromJson(Map<String, dynamic> j) => VatReportRow(
        ratePercent: _d(j['ratePercent']),
        taxableSales: _d(j['taxableSales']),
        vat: _d(j['vat']),
      );
}

/// VatReportDto.
class VatReport {
  final List<VatReportRow> rows;
  final double totalTaxable;
  final double totalVat;
  final String currency;

  VatReport({
    required this.rows,
    required this.totalTaxable,
    required this.totalVat,
    required this.currency,
  });

  factory VatReport.fromJson(Map<String, dynamic> j) => VatReport(
        rows: ((j['rows'] as List?) ?? [])
            .map((e) => VatReportRow.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalTaxable: _d(j['totalTaxable']),
        totalVat: _d(j['totalVat']),
        currency: j['currency'] == null ? 'Tk' : _s(j['currency']),
      );
}

/// GlAccountBalanceDto (used by VAT remittance screen).
class GlAccountBalance {
  final String code;
  final String name;
  final double debit;
  final double credit;
  final double balance;

  GlAccountBalance({
    required this.code,
    required this.name,
    required this.debit,
    required this.credit,
    required this.balance,
  });

  factory GlAccountBalance.fromJson(Map<String, dynamic> j) => GlAccountBalance(
        code: _s(j['code']),
        name: _s(j['name']),
        debit: _d(j['debit']),
        credit: _d(j['credit']),
        balance: _d(j['balance']),
      );
}

// ---------------------------------------------------------------------------
// reports — cash ledger (cash book + account ledger)
// ---------------------------------------------------------------------------

/// CashLedgerRowDto.
class CashLedgerRow {
  final DateTime occurredOn;
  final String number;
  final String categoryName;
  final String cashAccountName;
  final String type;
  final double inAmount;
  final double outAmount;
  final double runningBalance;

  CashLedgerRow({
    required this.occurredOn,
    required this.number,
    required this.categoryName,
    required this.cashAccountName,
    required this.type,
    required this.inAmount,
    required this.outAmount,
    required this.runningBalance,
  });

  factory CashLedgerRow.fromJson(Map<String, dynamic> j) => CashLedgerRow(
        occurredOn: _dt(j['occurredOn']),
        number: _s(j['number']),
        categoryName: _s(j['categoryName']),
        cashAccountName: _s(j['cashAccountName']),
        type: _s(j['type']),
        inAmount: _d(j['in']),
        outAmount: _d(j['out']),
        runningBalance: _d(j['runningBalance']),
      );
}

/// CashLedgerDto.
class CashLedger {
  final double openingBalance;
  final double totalReceipts;
  final double totalPayments;
  final double closingBalance;
  final List<CashLedgerRow> rows;

  CashLedger({
    required this.openingBalance,
    required this.totalReceipts,
    required this.totalPayments,
    required this.closingBalance,
    required this.rows,
  });

  factory CashLedger.fromJson(Map<String, dynamic> j) => CashLedger(
        openingBalance: _d(j['openingBalance']),
        totalReceipts: _d(j['totalReceipts']),
        totalPayments: _d(j['totalPayments']),
        closingBalance: _d(j['closingBalance']),
        rows: ((j['rows'] as List?) ?? [])
            .map((e) => CashLedgerRow.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

// ---------------------------------------------------------------------------
// general ledger — chart of accounts
// ---------------------------------------------------------------------------

/// AccountNodeDto (tree node).
class AccountNode {
  final String id;
  final String code;
  final String name;
  final String accountType; // Asset | Liability | Equity | Income | Expense
  final String normalBalance; // Debit | Credit
  final bool isPostable;
  final bool isActive;
  final List<AccountNode> children;

  AccountNode({
    required this.id,
    required this.code,
    required this.name,
    required this.accountType,
    required this.normalBalance,
    required this.isPostable,
    required this.isActive,
    required this.children,
  });

  factory AccountNode.fromJson(Map<String, dynamic> j) => AccountNode(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        accountType: _s(j['accountType']),
        normalBalance: _s(j['normalBalance']),
        isPostable: j['isPostable'] == true,
        isActive: j['isActive'] == true,
        children: ((j['children'] as List?) ?? [])
            .map((e) => AccountNode.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// AccountDto (flat — for journal-line pickers).
class GlAccount {
  final String id;
  final String code;
  final String name;
  final String accountType;
  final String normalBalance;
  final String? parentId;
  final bool isPostable;
  final bool isActive;
  final String? description;

  GlAccount({
    required this.id,
    required this.code,
    required this.name,
    required this.accountType,
    required this.normalBalance,
    this.parentId,
    required this.isPostable,
    required this.isActive,
    this.description,
  });

  factory GlAccount.fromJson(Map<String, dynamic> j) => GlAccount(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        accountType: _s(j['accountType']),
        normalBalance: _s(j['normalBalance']),
        parentId: _sOrNull(j['parentId']),
        isPostable: j['isPostable'] == true,
        isActive: j['isActive'] == true,
        description: _sOrNull(j['description']),
      );
}

// ---------------------------------------------------------------------------
// general ledger — journal entries
// ---------------------------------------------------------------------------

/// JournalEntryListItemDto.
class JournalEntryRow {
  final String id;
  final String entryNumber;
  final DateTime entryDate;
  final String voucherType;
  final String status; // Draft | Posted | Void
  final String? reference;
  final String? narration;
  final double totalDebit;
  final double totalCredit;
  final int lineCount;

  JournalEntryRow({
    required this.id,
    required this.entryNumber,
    required this.entryDate,
    required this.voucherType,
    required this.status,
    this.reference,
    this.narration,
    required this.totalDebit,
    required this.totalCredit,
    required this.lineCount,
  });

  factory JournalEntryRow.fromJson(Map<String, dynamic> j) => JournalEntryRow(
        id: _s(j['id']),
        entryNumber: _s(j['entryNumber']),
        entryDate: _dt(j['entryDate']),
        voucherType: _s(j['voucherType']),
        status: _s(j['status']),
        reference: _sOrNull(j['reference']),
        narration: _sOrNull(j['narration']),
        totalDebit: _d(j['totalDebit']),
        totalCredit: _d(j['totalCredit']),
        lineCount: _i(j['lineCount']),
      );
}

// ---------------------------------------------------------------------------
// general ledger — trial balance
// ---------------------------------------------------------------------------

/// TrialBalanceRowDto.
class TrialBalanceRow {
  final String accountId;
  final String code;
  final String name;
  final String accountType;
  final double debit;
  final double credit;

  TrialBalanceRow({
    required this.accountId,
    required this.code,
    required this.name,
    required this.accountType,
    required this.debit,
    required this.credit,
  });

  factory TrialBalanceRow.fromJson(Map<String, dynamic> j) => TrialBalanceRow(
        accountId: _s(j['accountId']),
        code: _s(j['code']),
        name: _s(j['name']),
        accountType: _s(j['accountType']),
        debit: _d(j['debit']),
        credit: _d(j['credit']),
      );
}

/// TrialBalanceDto.
class TrialBalance {
  final List<TrialBalanceRow> rows;
  final double totalDebit;
  final double totalCredit;
  final bool isBalanced;

  TrialBalance({
    required this.rows,
    required this.totalDebit,
    required this.totalCredit,
    required this.isBalanced,
  });

  factory TrialBalance.fromJson(Map<String, dynamic> j) => TrialBalance(
        rows: ((j['rows'] as List?) ?? [])
            .map((e) => TrialBalanceRow.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalDebit: _d(j['totalDebit']),
        totalCredit: _d(j['totalCredit']),
        isBalanced: j['isBalanced'] == true,
      );
}

// ---------------------------------------------------------------------------
// general ledger — P&L and balance sheet
// ---------------------------------------------------------------------------

/// GlAccountLineDto.
class GlAccountLine {
  final String code;
  final String name;
  final double amount;

  GlAccountLine({required this.code, required this.name, required this.amount});

  factory GlAccountLine.fromJson(Map<String, dynamic> j) => GlAccountLine(
        code: _s(j['code']),
        name: _s(j['name']),
        amount: _d(j['amount']),
      );
}

/// GlProfitAndLossDto.
class GlProfitAndLoss {
  final List<GlAccountLine> income;
  final double totalIncome;
  final List<GlAccountLine> expense;
  final double totalExpense;
  final double netProfit;

  GlProfitAndLoss({
    required this.income,
    required this.totalIncome,
    required this.expense,
    required this.totalExpense,
    required this.netProfit,
  });

  static List<GlAccountLine> _lines(dynamic v) =>
      ((v as List?) ?? []).map((e) => GlAccountLine.fromJson(e as Map<String, dynamic>)).toList();

  factory GlProfitAndLoss.fromJson(Map<String, dynamic> j) => GlProfitAndLoss(
        income: _lines(j['income']),
        totalIncome: _d(j['totalIncome']),
        expense: _lines(j['expense']),
        totalExpense: _d(j['totalExpense']),
        netProfit: _d(j['netProfit']),
      );
}

/// BalanceSheetDto.
class BalanceSheet {
  final List<GlAccountLine> assets;
  final double totalAssets;
  final List<GlAccountLine> liabilities;
  final double totalLiabilities;
  final List<GlAccountLine> equity;
  final double currentEarnings;
  final double totalEquity;
  final bool isBalanced;

  BalanceSheet({
    required this.assets,
    required this.totalAssets,
    required this.liabilities,
    required this.totalLiabilities,
    required this.equity,
    required this.currentEarnings,
    required this.totalEquity,
    required this.isBalanced,
  });

  static List<GlAccountLine> _lines(dynamic v) =>
      ((v as List?) ?? []).map((e) => GlAccountLine.fromJson(e as Map<String, dynamic>)).toList();

  factory BalanceSheet.fromJson(Map<String, dynamic> j) => BalanceSheet(
        assets: _lines(j['assets']),
        totalAssets: _d(j['totalAssets']),
        liabilities: _lines(j['liabilities']),
        totalLiabilities: _d(j['totalLiabilities']),
        equity: _lines(j['equity']),
        currentEarnings: _d(j['currentEarnings']),
        totalEquity: _d(j['totalEquity']),
        isBalanced: j['isBalanced'] == true,
      );
}
