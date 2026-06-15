import 'package:dio/dio.dart';
import '../models/dtos.dart';
import '../models/enums.dart';
import 'api_client.dart';

/// Typed wrapper over the `/waiter/*` REST surface (+ `/staff/auth/login`).
class WaiterApi {
  final ApiClient client;
  WaiterApi(this.client);

  Dio get _dio => client.dio;

  List<T> _list<T>(Response res, T Function(Map<String, dynamic>) f) =>
      (res.data as List).map((e) => f(e as Map<String, dynamic>)).toList();

  // ---------- auth ----------
  /// POST /staff/auth/login → { accessToken, expiresAtUtc, user{...} }.
  Future<Map<String, dynamic>> login(String emailOrUsername, String password) =>
      client.guard(() async {
        final res = await _dio.post('/staff/auth/login',
            data: {'emailOrUsername': emailOrUsername, 'password': password});
        return res.data as Map<String, dynamic>;
      });

  // ---------- console / reads ----------
  Future<WaiterConsole> console() => client.guard(() async {
        final res = await _dio.get('/waiter/console');
        return WaiterConsole.fromJson(res.data as Map<String, dynamic>);
      });

  Future<List<ActiveOrder>> activeOrders() => client.guard(() async {
        final res = await _dio.get('/waiter/orders/active');
        return _list(res, ActiveOrder.fromJson);
      });

  Future<OrderDetail> order(String id) => client.guard(() async {
        final res = await _dio.get('/waiter/orders/$id');
        return OrderDetail.fromJson(res.data as Map<String, dynamic>);
      });

  Future<SessionBill> sessionBill(String sessionId) => client.guard(() async {
        final res = await _dio.get('/waiter/sessions/$sessionId/bill');
        return SessionBill.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- catalog ----------
  Future<List<Product>> products() => client.guard(() async {
        final res = await _dio.get('/waiter/catalog/products');
        return _list(res, Product.fromJson);
      });

  Future<List<ProductCategory>> categories() => client.guard(() async {
        final res = await _dio.get('/waiter/catalog/categories');
        return _list(res, ProductCategory.fromJson);
      });

  Future<List<RestaurantTable>> tables() => client.guard(() async {
        final res = await _dio.get('/waiter/catalog/tables');
        return _list(res, RestaurantTable.fromJson);
      });

  Future<List<ProductAvailability>> availability() => client.guard(() async {
        final res = await _dio.get('/waiter/catalog/availability');
        return _list(res, ProductAvailability.fromJson);
      });

  Future<List<StaffUser>> staff() => client.guard(() async {
        final res = await _dio.get('/waiter/staff');
        return _list(res, StaffUser.fromJson);
      });

  // ---------- session actions ----------
  Future<OpenSessionResult> openSession(String tableId, int guestCount) =>
      client.guard(() async {
        final res = await _dio.post('/waiter/sessions/open',
            data: {'tableId': tableId, 'guestCount': guestCount});
        return OpenSessionResult.fromJson(res.data as Map<String, dynamic>);
      });

  Future<void> changeGuests(String sessionId, int guestCount) =>
      client.guard(() async {
        await _dio.post('/waiter/sessions/$sessionId/guests',
            data: {'guestCount': guestCount});
      });

  Future<void> moveTable(String sessionId, String targetTableId) =>
      client.guard(() async {
        await _dio.post('/waiter/sessions/$sessionId/move',
            data: {'targetTableId': targetTableId});
      });

  Future<void> mergeSessions(String survivorSessionId, List<String> sourceSessionIds) =>
      client.guard(() async {
        await _dio.post('/waiter/sessions/$survivorSessionId/merge',
            data: {'sourceSessionIds': sourceSessionIds});
      });

  Future<OpenSessionResult> splitSession(
          String sourceSessionId, List<String> orderIds, String targetTableId, int guestCount) =>
      client.guard(() async {
        final res = await _dio.post('/waiter/sessions/$sourceSessionId/split', data: {
          'orderIds': orderIds,
          'targetTableId': targetTableId,
          'guestCount': guestCount,
        });
        return OpenSessionResult.fromJson(res.data as Map<String, dynamic>);
      });

  Future<void> transferWaiter(String sessionId, String? waiterUserId, String? waiterName) =>
      client.guard(() async {
        await _dio.post('/waiter/sessions/$sessionId/transfer-waiter',
            data: {'waiterUserId': waiterUserId, 'waiterName': waiterName});
      });

  Future<void> requestPayment(String sessionId) => client.guard(() async {
        await _dio.post('/waiter/sessions/$sessionId/request-payment');
      });

  Future<void> closeSession(String sessionId, {String? reason}) =>
      client.guard(() async {
        await _dio.post('/waiter/sessions/$sessionId/close',
            data: {'reason': reason});
      });

  // ---------- orders ----------
  Future<PlaceOrderResult> placeOrder({
    String? customerPhone,
    String? customerName,
    String? tableId,
    required OrderType type,
    String? notes,
    required List<Map<String, dynamic>> lines,
    int? guestCount,
    String? diningSessionId,
  }) =>
      client.guard(() async {
        final res = await _dio.post('/waiter/orders', data: {
          'customerPhone': customerPhone,
          'customerName': customerName,
          'tableId': tableId,
          'type': type.wire,
          'notes': notes,
          'lines': lines,
          'guestCount': guestCount,
          'diningSessionId': diningSessionId,
        });
        return PlaceOrderResult.fromJson(res.data as Map<String, dynamic>);
      });

  Future<PlaceOrderResult> updateOrderLines(String orderId, List<Map<String, dynamic>> lines) =>
      client.guard(() async {
        final res = await _dio.put('/waiter/orders/$orderId/lines', data: {'lines': lines});
        return PlaceOrderResult.fromJson(res.data as Map<String, dynamic>);
      });

  Future<void> changeStatus(String orderId, OrderStatus target, {String? cancellationReason}) =>
      client.guard(() async {
        await _dio.post('/waiter/orders/$orderId/status',
            data: {'target': target.wire, 'cancellationReason': cancellationReason});
      });

  Future<void> resolveRequest(String id) => client.guard(() async {
        await _dio.post('/waiter/requests/$id/resolve');
      });

  // ---------- PDFs ----------
  /// Downloads a protected PDF (KOT or bill) as bytes — `url_launcher` can't
  /// attach the bearer header, so we fetch through the authenticated Dio client.
  Future<List<int>> pdfBytes(String path) => client.guard(() async {
        final res = await _dio.get<List<int>>(path,
            options: Options(responseType: ResponseType.bytes));
        return res.data ?? <int>[];
      });
}
