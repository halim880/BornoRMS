import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import 'store_api.dart';
import 'store_models.dart';

// ---------- dashboard ----------
final storeDashboardProvider = FutureProvider.autoDispose<StoreDashboard>(
    (ref) => ref.read(staffApiProvider).storeDashboard());

// ---------- items (paginated) ----------
final storeItemsPageProvider = StateProvider.autoDispose<int>((ref) => 1);

final storeItemsProvider = FutureProvider.autoDispose<Paged<StoreItem>>((ref) {
  final page = ref.watch(storeItemsPageProvider);
  return ref.read(staffApiProvider).storeItems(page: page, pageSize: 25);
});

// ---------- categories ----------
final storeCategoriesProvider = FutureProvider.autoDispose<List<StoreCategory>>(
    (ref) => ref.read(staffApiProvider).storeCategories());

// ---------- departments ----------
final storeDepartmentsProvider = FutureProvider.autoDispose<List<StoreDepartment>>(
    (ref) => ref.read(staffApiProvider).storeDepartments());

// ---------- suppliers ----------
final storeSuppliersProvider = FutureProvider.autoDispose<List<StoreSupplier>>(
    (ref) => ref.read(staffApiProvider).storeSuppliers());

// ---------- goods receipts (paginated) ----------
final storeGrnPageProvider = StateProvider.autoDispose<int>((ref) => 1);

final storeGoodsReceiptsProvider = FutureProvider.autoDispose<Paged<StoreGoodsReceipt>>((ref) {
  final page = ref.watch(storeGrnPageProvider);
  return ref.read(staffApiProvider).storeGoodsReceipts(page: page, pageSize: 25);
});

// ---------- requisitions (paginated) ----------
final storeRequisitionsPageProvider = StateProvider.autoDispose<int>((ref) => 1);

final storeRequisitionsProvider = FutureProvider.autoDispose<Paged<StoreRequisition>>((ref) {
  final page = ref.watch(storeRequisitionsPageProvider);
  return ref.read(staffApiProvider).storeRequisitions(page: page, pageSize: 25);
});

// ---------- issues (paginated) ----------
final storeIssuesPageProvider = StateProvider.autoDispose<int>((ref) => 1);

final storeIssuesProvider = FutureProvider.autoDispose<Paged<StoreIssue>>((ref) {
  final page = ref.watch(storeIssuesPageProvider);
  return ref.read(staffApiProvider).storeIssues(page: page, pageSize: 25);
});

// ---------- movement ledger (recent movements, all items) ----------
final storeLedgerProvider = FutureProvider.autoDispose<StoreMovementLedger>(
    (ref) => ref.read(staffApiProvider).storeLedger(take: 500));

// ---------- supplier payables ----------
final storePayablesProvider = FutureProvider.autoDispose<List<StoreSupplierPayable>>(
    (ref) => ref.read(staffApiProvider).storePayables());

// ---------- department consumption report ----------
/// Date-range preset for the department-issues report (reuses the shared range enum).
final storeReportRangeProvider = StateProvider.autoDispose<DashboardRange>((ref) => DashboardRange.last7Days);

final storeDepartmentIssuesProvider =
    FutureProvider.autoDispose<StoreDepartmentConsumption>((ref) {
  final w = ref.watch(storeReportRangeProvider).window();
  // Backend filters OccurredAtUtc < ToUtc, so make the upper bound exclusive of the next day.
  final to = DateTime(w.to.year, w.to.month, w.to.day).add(const Duration(days: 1));
  return ref.read(staffApiProvider).storeDepartmentIssues(from: w.from, to: to);
});
