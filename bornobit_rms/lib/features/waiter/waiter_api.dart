import '../../core/api/staff_api.dart';
import '../../core/models/dtos.dart';
import 'waiter_models.dart';

/// Waiter console HTTP surface — the legacy unversioned `/waiter/*` routes
/// (WaiterEndpoints.cs). Lives as an extension so the feature stays self-contained.
extension WaiterApi on StaffApi {
  // ---------- reads ----------
  Future<WaiterConsole> waiterConsole() => client.guard(() async {
        final res = await client.dio.get('/waiter/console');
        final j = res.data as Map<String, dynamic>;
        return WaiterConsole(
          dashboard: WaiterDashboard.fromJson(j['dashboard'] as Map<String, dynamic>),
          floorRaw: (j['floor'] as List? ?? []),
          ready: (j['ready'] as List? ?? [])
              .map((e) => ReadyToServeRow.fromJson(e as Map<String, dynamic>))
              .toList(),
          requestsRaw: (j['requests'] as List? ?? []),
        );
      });

  Future<List<SessionRow>> waiterMySessions() => client.guard(() async {
        final res = await client.dio.get('/waiter/my-sessions');
        return (res.data as List)
            .map((e) => SessionRow.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<SessionBill> waiterSessionBill(String sessionId) => client.guard(() async {
        final res = await client.dio.get('/waiter/sessions/$sessionId/bill');
        return SessionBill.fromJson(res.data as Map<String, dynamic>);
      });

  Future<OrderDetail> waiterOrder(String id) => client.guard(() async {
        final res = await client.dio.get('/waiter/orders/$id');
        return OrderDetail.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- session actions ----------
  Future<void> waiterOpenSession(String tableId, {int guestCount = 0}) =>
      client.guard(() async {
        await client.dio.post('/waiter/sessions/open',
            data: {'tableId': tableId, 'guestCount': guestCount});
      });

  Future<void> waiterSetGuests(String sessionId, int guestCount) => client.guard(() async {
        await client.dio.post('/waiter/sessions/$sessionId/guests', data: {'guestCount': guestCount});
      });

  Future<void> waiterMoveSession(String sessionId, String targetTableId) =>
      client.guard(() async {
        await client.dio.post('/waiter/sessions/$sessionId/move', data: {'targetTableId': targetTableId});
      });

  Future<void> waiterRequestPayment(String sessionId) => client.guard(() async {
        await client.dio.post('/waiter/sessions/$sessionId/request-payment');
      });

  Future<void> waiterCloseSession(String sessionId, {String? reason}) =>
      client.guard(() async {
        await client.dio.post('/waiter/sessions/$sessionId/close',
            data: {if (reason != null) 'reason': reason});
      });

  // ---------- orders ----------
  Future<void> waiterChangeStatus(String orderId, String target, {String? cancellationReason}) =>
      client.guard(() async {
        await client.dio.post('/waiter/orders/$orderId/status', data: {
          'target': target,
          if (cancellationReason != null) 'cancellationReason': cancellationReason,
        });
      });

  // ---------- requests ----------
  Future<void> waiterResolveRequest(String requestId) => client.guard(() async {
        await client.dio.post('/waiter/requests/$requestId/resolve');
      });
}
