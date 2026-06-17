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

  // ---------- orders ----------
  Future<Paged<OrderListItem>> orderList({String? status, bool? isPaid, int page = 1, int pageSize = 25}) =>
      client.guard(() async {
        final res = await _dio.get('$_p/staff/orders', queryParameters: {
          if (status != null) 'status': status,
          if (isPaid != null) 'isPaid': isPaid,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, OrderListItem.fromJson);
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
  }) =>
      client.guard(() async {
        final res = await _dio.post('$_pos/orders', data: {
          'type': type,
          if (tableId != null) 'tableId': tableId,
          if (customerPhone != null) 'customerPhone': customerPhone,
          if (customerName != null) 'customerName': customerName,
          if (customerAddress != null) 'customerAddress': customerAddress,
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

  /// payments: each map = { method, provider?, amount, tendered, reference? }
  Future<SettlementResult> posAddPayment(String id, List<Map<String, dynamic>> payments) =>
      client.guard(() async {
        final res = await _dio.post('$_pos/orders/$id/payments', data: {'payments': payments});
        return SettlementResult.fromJson(res.data as Map<String, dynamic>);
      });

  Future<void> posCancel(String id, {String? reason}) => client.guard(() async {
        await _dio.post('$_pos/orders/$id/cancel', data: {if (reason != null) 'reason': reason});
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
