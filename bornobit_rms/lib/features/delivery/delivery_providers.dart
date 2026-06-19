import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/realtime/live_connection.dart';
import 'delivery_api.dart';
import 'delivery_models.dart';

/// Board date filter (defaults to today). Null = all dates.
final deliveryDateProvider = StateProvider<DateTime?>((ref) => DateTime.now());

/// Live dispatch board. Reacts to delivery/order/payment ticks; 15s fallback poll.
final deliveryBoardProvider =
    AsyncNotifierProvider<DeliveryBoardNotifier, Paged<DeliveryBoardRow>>(DeliveryBoardNotifier.new);

class DeliveryBoardNotifier extends PollingNotifier<Paged<DeliveryBoardRow>> {
  @override
  List<String> get liveScopes => const [LiveScope.delivery, LiveScope.orders, LiveScope.payments];

  @override
  Future<Paged<DeliveryBoardRow>> fetch() {
    final date = ref.read(deliveryDateProvider);
    return ref.read(staffApiProvider).deliveryBoard(date: date, pageSize: 200);
  }
}

/// Rider roster (active by default).
final ridersProvider = FutureProvider.autoDispose<List<Rider>>((ref) {
  return ref.read(staffApiProvider).riders(includeInactive: true);
});

/// Per-rider COD reconciliation for the selected board date.
final codReconciliationProvider = FutureProvider.autoDispose<List<RiderCodRow>>((ref) {
  final date = ref.watch(deliveryDateProvider);
  return ref.read(staffApiProvider).codReconciliation(date: date);
});
