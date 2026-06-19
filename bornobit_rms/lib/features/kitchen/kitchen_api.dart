import '../../core/api/staff_api.dart';
import '../../core/config/app_config.dart';
import 'kitchen_models.dart';

/// Kitchen Display HTTP surface — the versioned `/api/v1/staff/kitchen/*` routes
/// (KitchenEndpoints.cs). Lives as an extension so the feature stays self-contained.
extension KitchenApi on StaffApi {
  String get _kds => '${AppConfig.apiPrefix}/staff/kitchen';

  // ---------- reads ----------
  /// Board + stations + metrics in one round-trip, with the active filters.
  Future<KitchenConsole> kitchenConsole({
    String? stationId,
    String? type,
    String? tableNumber,
    String? search,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_kds/console', queryParameters: {
          if (stationId != null) 'stationId': stationId,
          if (type != null) 'type': type,
          if (tableNumber != null && tableNumber.isNotEmpty) 'tableNumber': tableNumber,
          if (search != null && search.isNotEmpty) 'search': search,
        });
        return KitchenConsole.fromJson(res.data as Map<String, dynamic>);
      });

  Future<KitchenBoard> kitchenBoard({
    String? stationId,
    String? type,
    String? tableNumber,
    String? search,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_kds/board', queryParameters: {
          if (stationId != null) 'stationId': stationId,
          if (type != null) 'type': type,
          if (tableNumber != null && tableNumber.isNotEmpty) 'tableNumber': tableNumber,
          if (search != null && search.isNotEmpty) 'search': search,
        });
        return KitchenBoard.fromJson(res.data as Map<String, dynamic>);
      });

  Future<List<KitchenStation>> kitchenStations() => client.guard(() async {
        final res = await client.dio.get('$_kds/stations');
        return (res.data as List)
            .map((e) => KitchenStation.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<KitchenMetrics> kitchenMetrics() => client.guard(() async {
        final res = await client.dio.get('$_kds/metrics');
        return KitchenMetrics.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- order actions ----------
  /// Accept a still-Placed order (confirms + fires the kitchen ticket).
  Future<void> kitchenAccept(String orderId) => client.guard(() async {
        await client.dio.post('$_kds/orders/$orderId/accept');
      });

  /// Single-click advance through the fulfilment track (Confirmed → Preparing → Ready → Served).
  Future<void> kitchenAdvance(String orderId) => client.guard(() async {
        await client.dio.post('$_kds/orders/$orderId/advance');
      });

  /// Explicit status change (e.g. Preparing → Ready, or cancel) via the shared order command.
  Future<void> kitchenChangeStatus(String orderId, String target, {String? cancellationReason}) =>
      client.guard(() async {
        await client.dio.post('$_kds/orders/$orderId/status', data: {
          'target': target,
          if (cancellationReason != null) 'cancellationReason': cancellationReason,
        });
      });

  Future<void> kitchenTogglePriority(String orderId, bool isPriority) => client.guard(() async {
        await client.dio.post('$_kds/orders/$orderId/priority', data: {'isPriority': isPriority});
      });

  Future<void> kitchenSaveNotes(String orderId, String? notes) => client.guard(() async {
        await client.dio.post('$_kds/orders/$orderId/notes', data: {'notes': notes});
      });
}
