import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/realtime/live_connection.dart';
import 'waiter_api.dart';
import 'waiter_models.dart';

/// Typed, fully-parsed snapshot of the waiter console.
class WaiterConsoleData {
  final WaiterDashboard dashboard;
  final List<TableOverviewRow> floor;
  final List<ReadyToServeRow> ready;
  final List<CustomerRequestRow> requests;
  final List<SessionRow> mySessions;
  WaiterConsoleData({
    required this.dashboard,
    required this.floor,
    required this.ready,
    required this.requests,
    required this.mySessions,
  });
}

/// Polled console snapshot (`/waiter/console` + `/waiter/my-sessions`).
final waiterConsoleProvider =
    AsyncNotifierProvider<WaiterConsoleNotifier, WaiterConsoleData>(WaiterConsoleNotifier.new);

class WaiterConsoleNotifier extends PollingNotifier<WaiterConsoleData> {
  @override
  List<String> get liveScopes => const [
        LiveScope.orders,
        LiveScope.kitchen,
        LiveScope.sessions,
        LiveScope.tables,
        LiveScope.requests,
        LiveScope.payments,
      ];

  @override
  Future<WaiterConsoleData> fetch() async {
    final api = ref.read(staffApiProvider);
    final results = await Future.wait([
      api.waiterConsole(),
      api.waiterMySessions(),
    ]);
    final console = results[0] as WaiterConsole;
    final sessions = results[1] as List<SessionRow>;
    return WaiterConsoleData(
      dashboard: console.dashboard,
      floor: console.floorRaw
          .map((e) => TableOverviewRow.fromJson(e as Map<String, dynamic>))
          .toList(),
      ready: console.ready,
      requests: console.requestsRaw
          .map((e) => CustomerRequestRow.fromJson(e as Map<String, dynamic>))
          .toList(),
      mySessions: sessions,
    );
  }
}

/// Session bill detail, loaded on demand for the bill dialog.
final sessionBillProvider = FutureProvider.family<SessionBill, String>(
    (ref, sessionId) => ref.read(staffApiProvider).waiterSessionBill(sessionId));
