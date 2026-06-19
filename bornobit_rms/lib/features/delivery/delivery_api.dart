import '../../core/api/staff_api.dart';
import '../../core/config/app_config.dart';
import '../../core/models/dtos.dart';
import 'delivery_models.dart';

/// Typed wrappers over /api/v1/staff/delivery/* (LogisticsEndpoints.cs).
extension DeliveryApi on StaffApi {
  String get _base => '${AppConfig.apiPrefix}/staff/delivery';

  String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  Future<Paged<DeliveryBoardRow>> deliveryBoard({DateTime? date, bool unpaidOnly = false, int page = 1, int pageSize = 50}) =>
      client.guard(() async {
        final res = await client.dio.get('$_base/board', queryParameters: {
          if (date != null) 'date': _dateOnly(date),
          if (unpaidOnly) 'unpaidOnly': true,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, DeliveryBoardRow.fromJson);
      });

  Future<List<RiderCodRow>> codReconciliation({DateTime? date}) => client.guard(() async {
        final res = await client.dio.get('$_base/cod-reconciliation',
            queryParameters: {if (date != null) 'date': _dateOnly(date)});
        return (res.data as List).map((e) => RiderCodRow.fromJson(e as Map<String, dynamic>)).toList();
      });

  Future<void> assignRider(String orderId, String riderId) => client.guard(() async {
        await client.dio.post('$_base/$orderId/assign', data: {'riderId': riderId});
      });

  Future<void> markOutForDelivery(String orderId) => client.guard(() async {
        await client.dio.post('$_base/$orderId/out-for-delivery');
      });

  Future<void> markDelivered(String orderId) => client.guard(() async {
        await client.dio.post('$_base/$orderId/delivered');
      });

  Future<void> markDeliveryFailed(String orderId, String? reason) => client.guard(() async {
        await client.dio.post('$_base/$orderId/failed', data: {'reason': reason});
      });

  Future<void> cancelDelivery(String orderId, String? reason) => client.guard(() async {
        await client.dio.post('$_base/$orderId/cancel', data: {'reason': reason});
      });

  // ---------- riders ----------
  Future<List<Rider>> riders({bool includeInactive = false}) => client.guard(() async {
        final res = await client.dio.get('$_base/riders',
            queryParameters: {if (includeInactive) 'includeInactive': true});
        return (res.data as List).map((e) => Rider.fromJson(e as Map<String, dynamic>)).toList();
      });

  Future<void> createRider({required String name, required String phone, String? vehicle}) =>
      client.guard(() async {
        await client.dio.post('$_base/riders', data: {'name': name, 'phone': phone, 'vehicle': vehicle});
      });

  Future<void> updateRider({required String id, required String name, required String phone, String? vehicle}) =>
      client.guard(() async {
        await client.dio.put('$_base/riders/$id', data: {'name': name, 'phone': phone, 'vehicle': vehicle});
      });

  Future<void> setRiderActive(String id, bool active) => client.guard(() async {
        await client.dio.post('$_base/riders/$id/active', data: {'active': active});
      });
}
