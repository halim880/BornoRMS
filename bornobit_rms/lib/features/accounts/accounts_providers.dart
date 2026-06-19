import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import 'accounts_api.dart';
import 'accounts_models.dart';

/// Shared date-range preset for the Accounts report screens. Reuses the
/// dashboard's [DashboardRange] enum so the preset selector matches the rest of
/// the console.
final accountsRangeProvider = StateProvider<DashboardRange>((ref) => DashboardRange.thisMonth);

/// The business day shown on the day-end report (defaults to today).
final dayEndDateProvider = StateProvider<DateTime>((ref) {
  final n = DateTime.now();
  return DateTime(n.year, n.month, n.day);
});

({DateTime from, DateTime to}) _window(Ref ref) => ref.watch(accountsRangeProvider).window();

// ---------- transactions ----------
final transactionsPageProvider = StateProvider<int>((ref) => 1);

final transactionsProvider = FutureProvider.autoDispose<Paged<TransactionRow>>((ref) {
  final w = _window(ref);
  final page = ref.watch(transactionsPageProvider);
  return ref.read(staffApiProvider).transactions(from: w.from, to: w.to, page: page, pageSize: 25);
});

final transactionsSummaryProvider = FutureProvider.autoDispose<FinanceSummary>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).transactionsSummary(from: w.from, to: w.to);
});

// ---------- cash accounts / categories ----------
final cashAccountsProvider = FutureProvider.autoDispose<List<CashAccount>>(
    (ref) => ref.read(staffApiProvider).cashAccounts());

final financeCategoriesProvider = FutureProvider.autoDispose<List<FinanceCategory>>(
    (ref) => ref.read(staffApiProvider).categories());

// ---------- payables ----------
final payablesProvider = FutureProvider.autoDispose<List<PayableRow>>(
    (ref) => ref.read(staffApiProvider).payables());

final supplierPaymentsProvider = FutureProvider.autoDispose<List<SupplierPaymentRow>>(
    (ref) => ref.read(staffApiProvider).supplierPayments());

// ---------- periods / fixed assets / bank rec ----------
final periodsProvider =
    FutureProvider.autoDispose<List<FiscalPeriod>>((ref) => ref.read(staffApiProvider).periods());

final fixedAssetsProvider = FutureProvider.autoDispose<List<FixedAsset>>(
    (ref) => ref.read(staffApiProvider).fixedAssets());

final bankReconciliationsProvider = FutureProvider.autoDispose<List<BankReconciliation>>(
    (ref) => ref.read(staffApiProvider).bankReconciliations());

// ---------- payroll ----------
final employeesProvider =
    FutureProvider.autoDispose<List<Employee>>((ref) => ref.read(staffApiProvider).employees());

final payrollRunsProvider = FutureProvider.autoDispose<List<PayrollRunSummary>>(
    (ref) => ref.read(staffApiProvider).payrollRuns());

// ---------- reports ----------
final profitLossProvider = FutureProvider.autoDispose<ProfitAndLoss>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).profitLoss(from: w.from, to: w.to);
});

final dayEndProvider = FutureProvider.autoDispose<DayEndReport>((ref) {
  final date = ref.watch(dayEndDateProvider);
  return ref.read(staffApiProvider).dayEnd(date: date);
});

final foodCostProvider = FutureProvider.autoDispose<FoodCostReport>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).foodCost(from: w.from, to: w.to);
});

final vatReportProvider = FutureProvider.autoDispose<VatReport>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).vatReport(from: w.from, to: w.to);
});

final vatRemittanceProvider = FutureProvider.autoDispose<GlAccountBalance>(
    (ref) => ref.read(staffApiProvider).vatRemittance());

final cashBookProvider = FutureProvider.autoDispose<CashLedger>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).cashBook(from: w.from, to: w.to);
});

/// Account ledger requires picking a cash account; the screen owns the selection.
final ledgerAccountProvider = StateProvider<String?>((ref) => null);

final accountLedgerProvider = FutureProvider.autoDispose<CashLedger>((ref) {
  final w = _window(ref);
  final acc = ref.watch(ledgerAccountProvider);
  return ref.read(staffApiProvider).accountLedger(from: w.from, to: w.to, cashAccountId: acc);
});

// ---------- general ledger ----------
final chartOfAccountsProvider = FutureProvider.autoDispose<List<AccountNode>>(
    (ref) => ref.read(staffApiProvider).chartOfAccounts());

/// Postable + active accounts for the journal-entry line picker.
final postableAccountsProvider = FutureProvider.autoDispose<List<GlAccount>>(
    (ref) => ref.read(staffApiProvider).glAccounts(postableOnly: true, activeOnly: true));

final journalPageProvider = StateProvider<int>((ref) => 1);

final journalEntriesProvider = FutureProvider.autoDispose<Paged<JournalEntryRow>>((ref) {
  final w = _window(ref);
  final page = ref.watch(journalPageProvider);
  return ref.read(staffApiProvider).journalEntries(from: w.from, to: w.to, page: page, pageSize: 25);
});

final trialBalanceProvider = FutureProvider.autoDispose<TrialBalance>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).trialBalance(from: w.from, to: w.to);
});

final glProfitLossProvider = FutureProvider.autoDispose<GlProfitAndLoss>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).glProfitLoss(from: w.from, to: w.to);
});

final balanceSheetProvider = FutureProvider.autoDispose<BalanceSheet>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).balanceSheet(asOf: w.to);
});
