import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_client.dart';
import '../api/staff_api.dart';
import '../auth/auth_controller.dart';
import '../auth/token_store.dart';
import '../config/app_config.dart';
import '../models/dtos.dart';
import '../realtime/live_connection.dart';

// ---------- infrastructure ----------
final tokenStoreProvider = Provider<TokenStore>((ref) => TokenStore());

final apiClientProvider = Provider<ApiClient>((ref) {
  return ApiClient(
    tokenStore: ref.read(tokenStoreProvider),
    // Deferred: invoked only on a 401, after a refresh attempt has failed and the token cleared.
    onUnauthorized: () => ref.read(authControllerProvider.notifier).onUnauthorized(),
    // Deferred: invoked on a 401 to silently rotate the access token before giving up.
    refreshSession: () => ref.read(authControllerProvider.notifier).refreshSession(),
  );
});

final staffApiProvider = Provider<StaffApi>((ref) => StaffApi(ref.read(apiClientProvider)));

/// Gates polling — flipped off while the window is hidden.
final pollingEnabledProvider = StateProvider<bool>((ref) => true);

/// Sidebar collapse state for the shell.
final navCollapsedProvider = StateProvider<bool>((ref) => false);

// ---------- navigation menu (DB-driven, fetched once after auth) ----------
final menuProvider = FutureProvider<List<MenuItem>>((ref) => ref.read(staffApiProvider).menu());

/// Which module the content area currently shows (url + display title).
class NavSelection {
  final String url;
  final String title;
  const NavSelection(this.url, this.title);
}

const dashboardRoute = '/operations/dashboard';
const ordersRoute = '/orders';
const posRoute = '/pos';
const waiterRoute = '/waiter/orders';

final selectedNavProvider =
    StateProvider<NavSelection>((ref) => const NavSelection(dashboardRoute, 'Dashboard'));

// ---------- orders module ----------
/// All scoping for the back-office Orders screen. `status`/`page` drive the paged
/// list; `from`/`to`/`search`/`orderNumber` scope both the list and the KPI summary.
class OrdersFilter {
  final String? status;
  final int page;
  final DateTime? fromDate;
  final DateTime? toDate;
  final String? search;
  final String? orderNumber;

  const OrdersFilter({
    this.status,
    this.page = 1,
    this.fromDate,
    this.toDate,
    this.search,
    this.orderNumber,
  });

  /// copyWith with explicit clearing: pass `clearStatus`/`clearDates`/`clearOrderNumber`
  /// to reset a field to null (a plain null arg keeps the current value).
  OrdersFilter copyWith({
    String? status,
    int? page,
    DateTime? fromDate,
    DateTime? toDate,
    String? search,
    String? orderNumber,
    bool clearStatus = false,
    bool clearDates = false,
    bool clearOrderNumber = false,
  }) =>
      OrdersFilter(
        status: clearStatus ? null : (status ?? this.status),
        page: page ?? this.page,
        fromDate: clearDates ? null : (fromDate ?? this.fromDate),
        toDate: clearDates ? null : (toDate ?? this.toDate),
        search: search ?? this.search,
        orderNumber: clearOrderNumber ? null : (orderNumber ?? this.orderNumber),
      );
}

final ordersFilterProvider = StateProvider<OrdersFilter>((ref) => const OrdersFilter());

final ordersProvider = FutureProvider<Paged<OrderListItem>>((ref) {
  final f = ref.watch(ordersFilterProvider);
  return ref.read(staffApiProvider).orderList(
        status: f.status,
        page: f.page,
        from: f.fromDate,
        to: f.toDate,
        search: f.search,
        orderNumber: f.orderNumber,
        pageSize: 25,
      );
});

/// KPI tiles + per-status tab counts. Reacts only to the scoping fields (date /
/// search / order number) — not status or page — so the counts stay stable while
/// the user switches status tabs.
final ordersSummaryProvider = FutureProvider<OrdersSummary>((ref) {
  final f = ref.watch(ordersFilterProvider.select((f) => (
        f.fromDate,
        f.toDate,
        f.search,
        f.orderNumber,
      )));
  return ref.read(staffApiProvider).orderSummary(
        from: f.$1,
        to: f.$2,
        search: f.$3,
        orderNumber: f.$4,
      );
});

final orderDetailProvider =
    FutureProvider.family<OrderDetail, String>((ref, id) => ref.read(staffApiProvider).order(id));

// ---------- dashboard filters ----------
enum DashboardRange { today, yesterday, last7Days, thisMonth }

extension DashboardRangeLabel on DashboardRange {
  String get label => switch (this) {
        DashboardRange.today => 'Today',
        DashboardRange.yesterday => 'Yesterday',
        DashboardRange.last7Days => 'Last 7 days',
        DashboardRange.thisMonth => 'This month',
      };

  ({DateTime from, DateTime to}) window() {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    return switch (this) {
      DashboardRange.today => (from: today, to: today),
      DashboardRange.yesterday => (
          from: today.subtract(const Duration(days: 1)),
          to: today.subtract(const Duration(days: 1))
        ),
      DashboardRange.last7Days => (from: today.subtract(const Duration(days: 6)), to: today),
      DashboardRange.thisMonth => (from: DateTime(now.year, now.month, 1), to: today),
    };
  }
}

/// Analytics range; changing it re-fetches the dashboard.
final dashboardRangeProvider = StateProvider<DashboardRange>((ref) => DashboardRange.today);

/// Optional status filter for the live-orders table (null = all).
final ordersStatusFilterProvider = StateProvider<String?>((ref) => null);

// ---------- polling base ----------
abstract class PollingNotifier<T> extends AsyncNotifier<T> {
  Timer? _timer;

  Future<T> fetch();

  /// Real-time scopes this notifier reacts to. A live tick whose scope is in this
  /// list (or the wildcard [LiveScope.all]) triggers an immediate refresh, so the
  /// timer is only a fallback. Empty = react to [LiveScope.all] ticks only.
  List<String> get liveScopes => const [];

  @override
  Future<T> build() async {
    _timer = Timer.periodic(AppConfig.pollInterval, (_) => _tick());

    // Refresh the instant the server signals a relevant change — only mounted
    // notifiers are listening, so refreshes stay scoped to the active screens.
    ref.listen(liveTickProvider, (_, next) {
      final scope = next.valueOrNull;
      if (scope != null && _matchesScope(scope)) _tick();
    });

    ref.onDispose(() => _timer?.cancel());
    return fetch();
  }

  bool _matchesScope(String scope) =>
      scope == LiveScope.all || liveScopes.contains(scope);

  Future<void> _tick() async {
    if (!ref.read(pollingEnabledProvider)) return;
    try {
      state = AsyncData(await fetch());
    } catch (e, st) {
      // Keep the last good snapshot on a transient failure (e.g. a network blip)
      // so a busy floor never blanks; only surface an error if we have no data yet.
      if (!state.hasValue) state = AsyncError(e, st);
    }
  }

  /// Immediate refresh without flashing a loading state.
  Future<void> refresh() => _tick();
}

// ---------- dashboard (every section in one poll) ----------
final dashboardProvider =
    AsyncNotifierProvider<DashboardNotifier, DashboardData>(DashboardNotifier.new);

class DashboardNotifier extends PollingNotifier<DashboardData> {
  // The live ops view reflects every operational change.
  @override
  List<String> get liveScopes => const [
        LiveScope.orders,
        LiveScope.kitchen,
        LiveScope.sessions,
        LiveScope.tables,
        LiveScope.requests,
        LiveScope.payments,
        LiveScope.inventory,
      ];

  @override
  Future<DashboardData> fetch() async {
    final api = ref.read(staffApiProvider);
    final range = ref.read(dashboardRangeProvider).window();
    final status = ref.read(ordersStatusFilterProvider);

    // Fan out — the sections are independent reads.
    final results = await Future.wait([
      api.summary(),
      api.tables(),
      api.kitchen(),
      api.orders(status: status, pageSize: 20),
      api.requests(),
      api.inventoryAlerts(),
      api.staffActivity(),
      api.salesByHour(from: range.from, to: range.to),
      api.salesByCategory(from: range.from, to: range.to),
      api.topItems(from: range.from, to: range.to, count: 8),
      api.revenueBreakdown(from: range.from, to: range.to),
    ]);

    return DashboardData(
      summary: results[0] as DashboardSummary,
      tables: results[1] as List<TableOverviewRow>,
      kitchen: results[2] as KitchenPerformance,
      orders: results[3] as Paged<LiveOrderRow>,
      requests: results[4] as List<CustomerRequestRow>,
      inventory: results[5] as InventoryAlerts,
      staff: results[6] as List<StaffActivityRow>,
      salesByHour: results[7] as List<HourlySales>,
      salesByCategory: results[8] as List<CategorySales>,
      topItems: results[9] as List<TopItemRow>,
      revenue: results[10] as RevenueBreakdown,
    );
  }
}
