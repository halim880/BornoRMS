import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_client.dart';
import '../api/waiter_api.dart';
import '../auth/auth_controller.dart';
import '../auth/token_store.dart';
import '../config/app_config.dart';
import '../models/dtos.dart';

// ---------- infrastructure ----------
final tokenStoreProvider = Provider<TokenStore>((ref) => TokenStore());

final apiClientProvider = Provider<ApiClient>((ref) {
  return ApiClient(
    tokenStore: ref.read(tokenStoreProvider),
    // Deferred: invoked only on a 401, after the token has been cleared.
    onUnauthorized: () => ref.read(authControllerProvider.notifier).onUnauthorized(),
  );
});

final waiterApiProvider = Provider<WaiterApi>((ref) => WaiterApi(ref.read(apiClientProvider)));

/// Gates polling — flipped off while the app is backgrounded to save battery
/// and avoid burning the 30-minute token (see [AppLifecycleListener] in app.dart).
final pollingEnabledProvider = StateProvider<bool>((ref) => true);

/// Bottom-nav tab index for the shell (0 Floor · 1 Take order · 2 Ready · 3 Requests).
final selectedTabProvider = StateProvider<int>((ref) => 0);

/// Carries a table/session into the Take order tab when the waiter taps
/// "Take order" on a floor table (mirrors TakeOrderForTable in the Blazor page).
class TakeOrderTarget {
  final String tableId;
  final String tableNumber;
  final String? sessionId;
  final int guests;
  const TakeOrderTarget({
    required this.tableId,
    required this.tableNumber,
    this.sessionId,
    this.guests = 0,
  });
}

final takeOrderTargetProvider = StateProvider<TakeOrderTarget?>((ref) => null);

/// AsyncNotifier that re-fetches on [AppConfig.pollInterval] and exposes a manual
/// [refresh] (called immediately after any mutation, mirroring the Blazor
/// console's RefreshAllAsync).
abstract class PollingNotifier<T> extends AsyncNotifier<T> {
  Timer? _timer;

  Future<T> fetch();

  @override
  Future<T> build() async {
    _timer = Timer.periodic(AppConfig.pollInterval, (_) => _tick());
    ref.onDispose(() => _timer?.cancel());
    return fetch();
  }

  Future<void> _tick() async {
    if (!ref.read(pollingEnabledProvider)) return;
    try {
      state = AsyncData(await fetch());
    } catch (e, st) {
      state = AsyncError(e, st);
    }
  }

  /// Immediate refresh without flashing a loading state.
  Future<void> refresh() => _tick();
}

// ---------- console (dashboard + floor + ready + requests in one poll) ----------
final consoleProvider =
    AsyncNotifierProvider<ConsoleNotifier, WaiterConsole>(ConsoleNotifier.new);

class ConsoleNotifier extends PollingNotifier<WaiterConsole> {
  @override
  Future<WaiterConsole> fetch() => ref.read(waiterApiProvider).console();
}

// ---------- running orders (Take order tab) ----------
final activeOrdersProvider =
    AsyncNotifierProvider<ActiveOrdersNotifier, List<ActiveOrder>>(ActiveOrdersNotifier.new);

class ActiveOrdersNotifier extends PollingNotifier<List<ActiveOrder>> {
  @override
  Future<List<ActiveOrder>> fetch() => ref.read(waiterApiProvider).activeOrders();
}

// ---------- catalog (fetched once; refresh on demand) ----------
final productsProvider =
    FutureProvider<List<Product>>((ref) => ref.read(waiterApiProvider).products());

final categoriesProvider =
    FutureProvider<List<ProductCategory>>((ref) => ref.read(waiterApiProvider).categories());

final tablesProvider =
    FutureProvider<List<RestaurantTable>>((ref) => ref.read(waiterApiProvider).tables());

final availabilityProvider =
    FutureProvider<List<ProductAvailability>>((ref) => ref.read(waiterApiProvider).availability());

final staffProvider =
    FutureProvider<List<StaffUser>>((ref) => ref.read(waiterApiProvider).staff());

// ---------- on-demand reads ----------
final sessionBillProvider =
    FutureProvider.family<SessionBill, String>((ref, sessionId) =>
        ref.read(waiterApiProvider).sessionBill(sessionId));

final orderProvider =
    FutureProvider.family<OrderDetail, String>((ref, orderId) =>
        ref.read(waiterApiProvider).order(orderId));

/// Refresh everything that polls + the on-demand availability. Call after any
/// write so the UI reflects the change without waiting for the next tick.
Future<void> refreshConsole(WidgetRef ref) async {
  await ref.read(consoleProvider.notifier).refresh();
  await ref.read(activeOrdersProvider.notifier).refresh();
  ref.invalidate(availabilityProvider);
}
