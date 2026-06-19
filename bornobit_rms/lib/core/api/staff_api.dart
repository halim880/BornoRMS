import 'package:dio/dio.dart';
import '../config/app_config.dart';
import '../models/dtos.dart';
import '../../features/pos/pos_models.dart';
import 'api_client.dart';

/// Typed wrapper over the staff HTTP API. Auth is unversioned (legacy route);
/// dashboard reads live under the versioned group (/api/v1/staff/dashboard/*).
class StaffApi {
  final ApiClient client;
  StaffApi(this.client);

  Dio get _dio => client.dio;
  String get _p => AppConfig.apiPrefix; // /api/v1

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

  /// POST /staff/auth/refresh → rotates the refresh token, returns a fresh access token.
  Future<Map<String, dynamic>> refresh(String refreshToken) =>
      client.guard(() async {
        final res = await _dio.post('/staff/auth/refresh', data: {'refreshToken': refreshToken});
        return res.data as Map<String, dynamic>;
      });

  /// POST /staff/auth/logout → revokes the refresh token server-side (best-effort).
  Future<void> logout(String refreshToken) =>
      client.guard(() async {
        await _dio.post('/staff/auth/logout', data: {'refreshToken': refreshToken});
      });

  // ---------- orders ----------
  Future<Paged<OrderListItem>> orderList({
    String? status,
    bool? isPaid,
    DateTime? from,
    DateTime? to,
    String? search,
    String? orderNumber,
    int page = 1,
    int pageSize = 25,
  }) =>
      client.guard(() async {
        final res = await _dio.get('$_p/staff/orders', queryParameters: {
          if (status != null) 'status': status,
          if (isPaid != null) 'isPaid': isPaid,
          ..._range(from, to),
          if (search != null && search.isNotEmpty) 'search': search,
          if (orderNumber != null && orderNumber.isNotEmpty) 'orderNumber': orderNumber,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, OrderListItem.fromJson);
      });

  /// KPI tiles + per-status tab counts for the Orders screen. Scoped by the same
  /// date/search/order-number filters as [orderList] but never by status.
  Future<OrdersSummary> orderSummary({
    DateTime? from,
    DateTime? to,
    String? search,
    String? orderNumber,
  }) =>
      client.guard(() async {
        final res = await _dio.get('$_p/staff/orders/summary', queryParameters: {
          ..._range(from, to),
          if (search != null && search.isNotEmpty) 'search': search,
          if (orderNumber != null && orderNumber.isNotEmpty) 'orderNumber': orderNumber,
        });
        return OrdersSummary.fromJson(res.data as Map<String, dynamic>);
      });

  Future<OrderDetail> order(String id) => client.guard(() async {
        final res = await _dio.get('$_p/staff/orders/$id');
        return OrderDetail.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- POS ----------
  String get _pos => '$_p/staff/pos';

  Future<List<PosProduct>> posProducts() => client.guard(() async {
        final res = await _dio.get('$_pos/catalog/products');
        return _list(res, PosProduct.fromJson);
      });

  Future<List<PosCategory>> posCategories() => client.guard(() async {
        final res = await _dio.get('$_pos/catalog/categories');
        return _list(res, PosCategory.fromJson);
      });

  Future<List<PosAvailability>> posAvailability() => client.guard(() async {
        final res = await _dio.get('$_pos/catalog/availability');
        return _list(res, PosAvailability.fromJson);
      });

  Future<List<PosOptionGroup>> posOptionGroups(String productId) => client.guard(() async {
        final res = await _dio.get('$_pos/catalog/products/$productId/option-groups');
        return _list(res, PosOptionGroup.fromJson);
      });

  Future<List<PosTable>> posTables() => client.guard(() async {
        final res = await _dio.get('$_pos/tables');
        return _list(res, PosTable.fromJson);
      });

  Future<List<ActiveOrder>> posActiveOrders() => client.guard(() async {
        final res = await _dio.get('$_pos/orders/active');
        return _list(res, ActiveOrder.fromJson);
      });

  Future<PlaceOrderResult> posCreateOrder({
    required String type,
    String? tableId,
    String? customerPhone,
    String? customerName,
    String? customerAddress,
    double? deliveryCharge,
  }) =>
      client.guard(() async {
        final res = await _dio.post('$_pos/orders', data: {
          'type': type,
          if (tableId != null) 'tableId': tableId,
          if (customerPhone != null) 'customerPhone': customerPhone,
          if (customerName != null) 'customerName': customerName,
          if (customerAddress != null) 'customerAddress': customerAddress,
          if (deliveryCharge != null) 'deliveryCharge': deliveryCharge,
        });
        return PlaceOrderResult.fromJson(res.data as Map<String, dynamic>);
      });

  Future<PlaceOrderResult> posUpdateOrder(
    String id, {
    required String type,
    String? tableId,
    String? customerPhone,
    String? customerName,
    String? customerAddress,
  }) =>
      client.guard(() async {
        final res = await _dio.patch('$_pos/orders/$id', data: {
          'type': type,
          if (tableId != null) 'tableId': tableId,
          if (customerPhone != null) 'customerPhone': customerPhone,
          if (customerName != null) 'customerName': customerName,
          if (customerAddress != null) 'customerAddress': customerAddress,
        });
        return PlaceOrderResult.fromJson(res.data as Map<String, dynamic>);
      });

  /// lines: each map = { menuItemId, quantity, variantId?, notes?, optionIds? }
  Future<PlaceOrderResult> posSetLines(String id, List<Map<String, dynamic>> lines) =>
      client.guard(() async {
        final res = await _dio.put('$_pos/orders/$id/lines', data: {'lines': lines});
        return PlaceOrderResult.fromJson(res.data as Map<String, dynamic>);
      });

  Future<BillSummary> posDiscount(String id, {double? percent, double? amount, String? reason}) =>
      client.guard(() async {
        final res = await _dio.post('$_pos/orders/$id/discount', data: {
          if (percent != null) 'percent': percent,
          if (amount != null) 'amount': amount,
          if (reason != null) 'reason': reason,
        });
        return BillSummary.fromJson(res.data as Map<String, dynamic>);
      });

  Future<BillSummary> posRounding(String id, String mode) => client.guard(() async {
        final res = await _dio.post('$_pos/orders/$id/rounding', data: {'mode': mode});
        return BillSummary.fromJson(res.data as Map<String, dynamic>);
      });

  /// payments: each map = { method, provider?, amount, tendered, reference? }.
  /// [idempotencyKey] dedups a replayed settle (double-tap / retry) so a tender is never double-charged.
  Future<SettlementResult> posAddPayment(String id, List<Map<String, dynamic>> payments, {String? idempotencyKey}) =>
      client.guard(() async {
        final res = await _dio.post('$_pos/orders/$id/payments', data: {
          'payments': payments,
          if (idempotencyKey != null) 'idempotencyKey': idempotencyKey,
        });
        return SettlementResult.fromJson(res.data as Map<String, dynamic>);
      });

  Future<void> posCancel(String id, {String? reason}) => client.guard(() async {
        await _dio.post('$_pos/orders/$id/cancel', data: {if (reason != null) 'reason': reason});
      });

  /// Void a mistaken captured payment. Manager-gated server-side; pass manager creds when the
  /// signed-in cashier lacks the role (else they're ignored and the role on the till authorizes it).
  Future<SettlementResult> posVoidPayment(String orderId, String paymentId,
          {required String reason, String? managerUserName, String? managerPassword}) =>
      client.guard(() async {
        final res = await _dio.post('$_pos/orders/$orderId/payments/$paymentId/void', data: {
          'reason': reason,
          if (managerUserName != null) 'managerUserName': managerUserName,
          if (managerPassword != null) 'managerPassword': managerPassword,
        });
        return SettlementResult.fromJson(res.data as Map<String, dynamic>);
      });

  /// Refund part or all of a captured payment. Manager-gated server-side (see [posVoidPayment]).
  Future<SettlementResult> posRefundPayment(String orderId, String paymentId,
          {required double amount, required String reason, String? managerUserName, String? managerPassword}) =>
      client.guard(() async {
        final res = await _dio.post('$_pos/orders/$orderId/payments/$paymentId/refund', data: {
          'amount': amount,
          'reason': reason,
          if (managerUserName != null) 'managerUserName': managerUserName,
          if (managerPassword != null) 'managerPassword': managerPassword,
        });
        return SettlementResult.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- cash drawer / shift ----------
  /// The current cashier's open drawer, or null if none is open.
  Future<CashDrawer?> drawerCurrent() => client.guard(() async {
        final res = await _dio.get('$_pos/drawers/current');
        return res.data == null ? null : CashDrawer.fromJson(res.data as Map<String, dynamic>);
      });

  Future<DrawerSummary> drawerSummary(String id) => client.guard(() async {
        final res = await _dio.get('$_pos/drawers/$id/summary');
        return DrawerSummary.fromJson(res.data as Map<String, dynamic>);
      });

  Future<CashDrawer> drawerOpen({required double openingBalance, String? cashAccountId, String? notes}) =>
      client.guard(() async {
        final res = await _dio.post('$_pos/drawers/open', data: {
          'openingBalance': openingBalance,
          if (cashAccountId != null) 'cashAccountId': cashAccountId,
          if (notes != null) 'notes': notes,
        });
        return CashDrawer.fromJson(res.data as Map<String, dynamic>);
      });

  Future<DrawerCloseResult> drawerClose(String id, {required double countedBalance, String? notes}) =>
      client.guard(() async {
        final res = await _dio.post('$_pos/drawers/$id/close', data: {
          'countedBalance': countedBalance,
          if (notes != null) 'notes': notes,
        });
        return DrawerCloseResult.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- navigation ----------
  Future<List<MenuItem>> menu() => client.guard(() async {
        final res = await _dio.get('$_p/staff/menu');
        return _list(res, MenuItem.fromJson);
      });

  // ---------- dashboard reads ----------
  Future<DashboardSummary> summary() => client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/summary');
        return DashboardSummary.fromJson(res.data as Map<String, dynamic>);
      });

  Future<List<TableOverviewRow>> tables() => client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/tables');
        return _list(res, TableOverviewRow.fromJson);
      });

  Future<KitchenPerformance> kitchen() => client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/kitchen');
        return KitchenPerformance.fromJson(res.data as Map<String, dynamic>);
      });

  Future<Paged<LiveOrderRow>> orders({String? status, int page = 1, int pageSize = 20}) =>
      client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/orders', queryParameters: {
          if (status != null) 'status': status,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, LiveOrderRow.fromJson);
      });

  Future<List<CustomerRequestRow>> requests() => client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/requests');
        return _list(res, CustomerRequestRow.fromJson);
      });

  Future<InventoryAlerts> inventoryAlerts() => client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/inventory-alerts');
        return InventoryAlerts.fromJson(res.data as Map<String, dynamic>);
      });

  Future<List<StaffActivityRow>> staffActivity() => client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/staff-activity');
        return _list(res, StaffActivityRow.fromJson);
      });

  Future<List<HourlySales>> salesByHour({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/sales-by-hour',
            queryParameters: _range(from, to));
        return _list(res, HourlySales.fromJson);
      });

  Future<List<CategorySales>> salesByCategory({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/sales-by-category',
            queryParameters: _range(from, to));
        return _list(res, CategorySales.fromJson);
      });

  Future<List<TopItemRow>> topItems({DateTime? from, DateTime? to, int count = 8}) =>
      client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/top-items',
            queryParameters: {..._range(from, to), 'count': count});
        return _list(res, TopItemRow.fromJson);
      });

  Future<RevenueBreakdown> revenueBreakdown({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await _dio.get('$_p/staff/dashboard/revenue-breakdown',
            queryParameters: _range(from, to));
        return RevenueBreakdown.fromJson(res.data as Map<String, dynamic>);
      });

  // Send a plain calendar date — the server takes `.Date`, so a UTC shift here
  // could move the window by a day.
  Map<String, dynamic> _range(DateTime? from, DateTime? to) => {
        if (from != null) 'from': _dateOnly(from),
        if (to != null) 'to': _dateOnly(to),
      };

  String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
}
