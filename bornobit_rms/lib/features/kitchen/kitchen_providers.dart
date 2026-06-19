import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/realtime/live_connection.dart';
import 'kitchen_api.dart';
import 'kitchen_models.dart';

/// Active board filters. Mutating any of these re-fetches the board on the next
/// poll tick (or immediately, via [kitchenBoardProvider.notifier.refresh]).
final kitchenStationFilterProvider = StateProvider<String?>((ref) => null); // null = All stations
final kitchenTypeFilterProvider = StateProvider<String?>((ref) => null); // null = All types
final kitchenTableFilterProvider = StateProvider<String>((ref) => '');
final kitchenSearchFilterProvider = StateProvider<String>((ref) => '');

/// Live kitchen console (`/staff/kitchen/console`): board + stations + metrics,
/// polled on the shared cadence.
final kitchenBoardProvider =
    AsyncNotifierProvider<KitchenBoardNotifier, KitchenConsole>(KitchenBoardNotifier.new);

class KitchenBoardNotifier extends PollingNotifier<KitchenConsole> {
  @override
  List<String> get liveScopes => const [LiveScope.orders, LiveScope.kitchen];

  @override
  Future<KitchenConsole> fetch() async {
    final api = ref.read(staffApiProvider);
    return api.kitchenConsole(
      stationId: ref.read(kitchenStationFilterProvider),
      type: ref.read(kitchenTypeFilterProvider),
      tableNumber: ref.read(kitchenTableFilterProvider),
      search: ref.read(kitchenSearchFilterProvider),
    );
  }
}
