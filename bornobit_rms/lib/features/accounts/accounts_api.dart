import '../../core/api/staff_api.dart';
import '../../core/config/app_config.dart';
import '../../core/models/dtos.dart';
import 'accounts_models.dart';

/// Typed wrappers over the /api/v1/staff/accounts/* surface (AccountsEndpoints.cs).
/// Dates are sent as plain calendar dates `yyyy-MM-dd` so a client-side UTC shift
/// cannot move the window by a day (the server takes `.Date`).
extension AccountsApi on StaffApi {
  String get _base => '${AppConfig.apiPrefix}/staff/accounts';

  String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  Map<String, dynamic> _range(DateTime? from, DateTime? to) => {
        if (from != null) 'from': _dateOnly(from),
        if (to != null) 'to': _dateOnly(to),
      };

  List<T> _mapList<T>(dynamic data, T Function(Map<String, dynamic>) f) =>
      (data as List).map((e) => f(e as Map<String, dynamic>)).toList();

  // ---------- transactions ----------
  Future<Paged<TransactionRow>> transactions({
    DateTime? from,
    DateTime? to,
    String? type,
    String? categoryId,
    String? cashAccountId,
    int page = 1,
    int pageSize = 50,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_base/transactions', queryParameters: {
          ..._range(from, to),
          if (type != null) 'type': type,
          if (categoryId != null) 'categoryId': categoryId,
          if (cashAccountId != null) 'cashAccountId': cashAccountId,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, TransactionRow.fromJson);
      });

  Future<FinanceSummary> transactionsSummary({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_base/transactions/summary', queryParameters: _range(from, to));
        return FinanceSummary.fromJson(res.data as Map<String, dynamic>);
      });

  Future<void> createTransaction({
    required DateTime occurredOn,
    required String type,
    required String cashAccountId,
    required String categoryId,
    required double amount,
    String? reference,
    String? notes,
  }) =>
      client.guard(() async {
        await client.dio.post('$_base/transactions', data: {
          'occurredOn': occurredOn.toIso8601String(),
          'type': type,
          'cashAccountId': cashAccountId,
          'categoryId': categoryId,
          'amount': amount,
          if (reference != null) 'reference': reference,
          if (notes != null) 'notes': notes,
        });
      });

  // ---------- cash accounts ----------
  Future<List<CashAccount>> cashAccounts({bool? activeOnly}) => client.guard(() async {
        final res = await client.dio.get('$_base/cash-accounts',
            queryParameters: {if (activeOnly != null) 'activeOnly': activeOnly});
        return _mapList(res.data, CashAccount.fromJson);
      });

  Future<void> createCashAccount({
    required String name,
    required String kind,
    required double openingBalance,
  }) =>
      client.guard(() async {
        await client.dio.post('$_base/cash-accounts',
            data: {'name': name, 'kind': kind, 'openingBalance': openingBalance});
      });

  // ---------- cash-counter → GL import ----------
  /// Post a day's un-accounted takings to the GL on demand (the background service also does this at
  /// day-end). Idempotent — re-running posts only what's still un-accounted. Defaults to today.
  Future<CashImportResult> cashCounterImport({DateTime? date}) => client.guard(() async {
        final res = await client.dio.post('$_base/cash-counter/import',
            data: {if (date != null) 'date': _dateOnly(date)});
        return CashImportResult.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- categories ----------
  Future<List<FinanceCategory>> categories({String? type, bool? activeOnly}) =>
      client.guard(() async {
        final res = await client.dio.get('$_base/categories', queryParameters: {
          if (type != null) 'type': type,
          if (activeOnly != null) 'activeOnly': activeOnly,
        });
        return _mapList(res.data, FinanceCategory.fromJson);
      });

  Future<void> createCategory({required String name, required String type}) =>
      client.guard(() async {
        await client.dio.post('$_base/categories', data: {'name': name, 'type': type});
      });

  // ---------- payables ----------
  Future<List<PayableRow>> payables({bool? outstandingOnly}) => client.guard(() async {
        final res = await client.dio.get('$_base/payables',
            queryParameters: {if (outstandingOnly != null) 'outstandingOnly': outstandingOnly});
        return _mapList(res.data, PayableRow.fromJson);
      });

  Future<List<SupplierPaymentRow>> supplierPayments({String? supplierId}) =>
      client.guard(() async {
        final res = await client.dio.get('$_base/payables/payments',
            queryParameters: {if (supplierId != null) 'supplierId': supplierId});
        return _mapList(res.data, SupplierPaymentRow.fromJson);
      });

  /// Record a payment against a supplier's balance: Dr Accounts Payable / Cr the chosen cash account.
  Future<void> recordSupplierPayment({
    required String supplierId,
    required String cashAccountId,
    required DateTime paidOn,
    required double amount,
    String? reference,
    String? notes,
  }) =>
      client.guard(() async {
        await client.dio.post('$_base/payables/record-payment', data: {
          'supplierId': supplierId,
          'cashAccountId': cashAccountId,
          'paidOn': paidOn.toUtc().toIso8601String(),
          'amount': amount,
          if (reference != null) 'reference': reference,
          if (notes != null) 'notes': notes,
        });
      });

  // ---------- periods ----------
  Future<List<FiscalPeriod>> periods() => client.guard(() async {
        final res = await client.dio.get('$_base/periods');
        return _mapList(res.data, FiscalPeriod.fromJson);
      });

  // ---------- fixed assets ----------
  Future<List<FixedAsset>> fixedAssets({bool? activeOnly}) => client.guard(() async {
        final res = await client.dio.get('$_base/fixed-assets',
            queryParameters: {if (activeOnly != null) 'activeOnly': activeOnly});
        return _mapList(res.data, FixedAsset.fromJson);
      });

  // ---------- bank reconciliation ----------
  Future<List<BankReconciliation>> bankReconciliations({String? cashAccountId}) =>
      client.guard(() async {
        final res = await client.dio.get('$_base/bank-rec',
            queryParameters: {if (cashAccountId != null) 'cashAccountId': cashAccountId});
        return _mapList(res.data, BankReconciliation.fromJson);
      });

  // ---------- payroll ----------
  Future<List<Employee>> employees({bool? activeOnly}) => client.guard(() async {
        final res = await client.dio.get('$_base/payroll/employees',
            queryParameters: {if (activeOnly != null) 'activeOnly': activeOnly});
        return _mapList(res.data, Employee.fromJson);
      });

  Future<List<PayrollRunSummary>> payrollRuns() => client.guard(() async {
        final res = await client.dio.get('$_base/payroll/runs');
        return _mapList(res.data, PayrollRunSummary.fromJson);
      });

  // ---------- reports ----------
  Future<ProfitAndLoss> profitLoss({DateTime? from, DateTime? to}) => client.guard(() async {
        final res = await client.dio
            .get('$_base/reports/profit-loss', queryParameters: _range(from, to));
        return ProfitAndLoss.fromJson(res.data as Map<String, dynamic>);
      });

  Future<DayEndReport> dayEnd({DateTime? date}) => client.guard(() async {
        final res = await client.dio.get('$_base/reports/day-end',
            queryParameters: {if (date != null) 'date': _dateOnly(date)});
        return DayEndReport.fromJson(res.data as Map<String, dynamic>);
      });

  Future<FoodCostReport> foodCost({DateTime? from, DateTime? to}) => client.guard(() async {
        final res =
            await client.dio.get('$_base/reports/food-cost', queryParameters: _range(from, to));
        return FoodCostReport.fromJson(res.data as Map<String, dynamic>);
      });

  Future<VatReport> vatReport({DateTime? from, DateTime? to}) => client.guard(() async {
        final res = await client.dio.get('$_base/reports/vat', queryParameters: _range(from, to));
        return VatReport.fromJson(res.data as Map<String, dynamic>);
      });

  Future<GlAccountBalance> vatRemittance() => client.guard(() async {
        final res = await client.dio.get('$_base/reports/vat-remittance');
        return GlAccountBalance.fromJson(res.data as Map<String, dynamic>);
      });

  Future<CashLedger> cashBook({DateTime? from, DateTime? to, String? cashAccountId}) =>
      client.guard(() async {
        final res = await client.dio.get('$_base/reports/cash-book', queryParameters: {
          ..._range(from, to),
          if (cashAccountId != null) 'cashAccountId': cashAccountId,
        });
        return CashLedger.fromJson(res.data as Map<String, dynamic>);
      });

  Future<CashLedger> accountLedger({DateTime? from, DateTime? to, String? cashAccountId}) =>
      client.guard(() async {
        final res = await client.dio.get('$_base/reports/ledger', queryParameters: {
          ..._range(from, to),
          if (cashAccountId != null) 'cashAccountId': cashAccountId,
        });
        return CashLedger.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- general ledger ----------
  Future<List<AccountNode>> chartOfAccounts({bool? activeOnly}) => client.guard(() async {
        final res = await client.dio.get('$_base/gl/chart',
            queryParameters: {if (activeOnly != null) 'activeOnly': activeOnly});
        return _mapList(res.data, AccountNode.fromJson);
      });

  Future<List<GlAccount>> glAccounts({bool? postableOnly, bool? activeOnly}) =>
      client.guard(() async {
        final res = await client.dio.get('$_base/gl/accounts', queryParameters: {
          if (postableOnly != null) 'postableOnly': postableOnly,
          if (activeOnly != null) 'activeOnly': activeOnly,
        });
        return _mapList(res.data, GlAccount.fromJson);
      });

  Future<Paged<JournalEntryRow>> journalEntries({
    DateTime? from,
    DateTime? to,
    String? voucherType,
    String? status,
    int page = 1,
    int pageSize = 50,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_base/gl/journal', queryParameters: {
          ..._range(from, to),
          if (voucherType != null) 'voucherType': voucherType,
          if (status != null) 'status': status,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, JournalEntryRow.fromJson);
      });

  /// lines: each map = { accountId, debit, credit, narration? }
  Future<void> createJournalEntry({
    required DateTime entryDate,
    required String voucherType,
    String? reference,
    String? narration,
    required List<Map<String, dynamic>> lines,
    bool postImmediately = false,
  }) =>
      client.guard(() async {
        await client.dio.post('$_base/gl/journal', data: {
          'entryDate': entryDate.toIso8601String(),
          'voucherType': voucherType,
          if (reference != null) 'reference': reference,
          if (narration != null) 'narration': narration,
          'lines': lines,
          'postImmediately': postImmediately,
        });
      });

  Future<TrialBalance> trialBalance({DateTime? from, DateTime? to}) => client.guard(() async {
        final res =
            await client.dio.get('$_base/gl/trial-balance', queryParameters: _range(from, to));
        return TrialBalance.fromJson(res.data as Map<String, dynamic>);
      });

  Future<GlProfitAndLoss> glProfitLoss({DateTime? from, DateTime? to}) => client.guard(() async {
        final res =
            await client.dio.get('$_base/gl/profit-loss', queryParameters: _range(from, to));
        return GlProfitAndLoss.fromJson(res.data as Map<String, dynamic>);
      });

  Future<BalanceSheet> balanceSheet({DateTime? asOf}) => client.guard(() async {
        final res = await client.dio.get('$_base/gl/balance-sheet',
            queryParameters: {if (asOf != null) 'asOf': _dateOnly(asOf)});
        return BalanceSheet.fromJson(res.data as Map<String, dynamic>);
      });
}
