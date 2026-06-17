import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_client.dart';
import '../api/staff_api.dart';
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

final selectedNavProvider =
    StateProvider<NavSelection>((ref) => const NavSelection(dashboardRoute, 'Dashboard'));

// ---------- orders module ----------
final ordersStatusProvider = StateProvider<String?>((ref) => null);
final ordersPageProvider = StateProvider<int>((ref) => 1);

final ordersProvider = FutureProvider<Paged<OrderListItem>>((ref) {
  final status = ref.watch(ordersStatusProvider);
  final page = ref.watch(ordersPageProvider);
  return ref.read(staffApiProvider).orderList(status: status, page: page, pageSize: 25);
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

// ---------- dashboard (every section in one poll) ----------
final dashboardProvider =
    AsyncNotifierProvider<DashboardNotifier, DashboardData>(DashboardNotifier.new);

class DashboardNotifier extends PollingNotifier<DashboardData> {
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
