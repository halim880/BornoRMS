import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import 'reports_api.dart';
import 'reports_models.dart';

/// Shared date-range preset for every report screen. Reuses the dashboard's
/// [DashboardRange] enum (Today / Yesterday / Last 7 days / This month) so the
/// preset selector matches the rest of the console.
final reportsRangeProvider = StateProvider<DashboardRange>((ref) => DashboardRange.today);

/// Top-N to fetch for the most-selling-items report.
final topItemsCountProvider = StateProvider<int>((ref) => 20);

({DateTime from, DateTime to}) _window(Ref ref) => ref.watch(reportsRangeProvider).window();

final salesReportProvider = FutureProvider.autoDispose<SalesReport>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).salesReport(from: w.from, to: w.to);
});

final salesInvoiceReportProvider = FutureProvider.autoDispose<SalesInvoiceReport>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).salesInvoiceReport(from: w.from, to: w.to);
});

final collectionReportProvider = FutureProvider.autoDispose<CollectionReport>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).collectionReport(from: w.from, to: w.to);
});

final topItemsReportProvider = FutureProvider.autoDispose<List<TopSellingItemRow>>((ref) {
  final w = _window(ref);
  final top = ref.watch(topItemsCountProvider);
  return ref.read(staffApiProvider).topSellingItems(from: w.from, to: w.to, top: top);
});

final categorySalesReportProvider = FutureProvider.autoDispose<List<CategorySalesRow>>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).categorySales(from: w.from, to: w.to);
});

final cashierReportProvider = FutureProvider.autoDispose<CashierReport>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).cashierReport(from: w.from, to: w.to);
});

final purchaseReportProvider = FutureProvider.autoDispose<PurchaseReport>((ref) {
  final w = _window(ref);
  return ref.read(staffApiProvider).purchaseReport(from: w.from, to: w.to);
});

/// Point-in-time snapshot — does not depend on the range selector.
final stockValuationReportProvider = FutureProvider.autoDispose<StockValuation>((ref) {
  return ref.read(staffApiProvider).stockValuation();
});
