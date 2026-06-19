import '../../core/api/staff_api.dart';
import '../../core/config/app_config.dart';
import '../../core/models/dtos.dart';
import 'store_models.dart';

/// Store / Warehouse HTTP surface — the versioned `/api/v1/staff/store/*` routes
/// (StoreEndpoints.cs). Read-only for this pass. Lives as an extension so the
/// feature stays self-contained.
extension StoreApi on StaffApi {
  String get _storeBase => '${AppConfig.apiPrefix}/staff/store';

  // ---------- dashboard ----------
  Future<StoreDashboard> storeDashboard() => client.guard(() async {
        final res = await client.dio.get('$_storeBase/dashboard');
        return StoreDashboard.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- items ----------
  Future<Paged<StoreItem>> storeItems({
    String? search,
    String? categoryId,
    bool? lowStockOnly,
    bool? includeInactive,
    int page = 1,
    int pageSize = 50,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_storeBase/items', queryParameters: {
          if (search != null && search.isNotEmpty) 'search': search,
          if (categoryId != null) 'categoryId': categoryId,
          if (lowStockOnly != null) 'lowStockOnly': lowStockOnly,
          if (includeInactive != null) 'includeInactive': includeInactive,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, StoreItem.fromJson);
      });

  // ---------- categories ----------
  Future<List<StoreCategory>> storeCategories() => client.guard(() async {
        final res = await client.dio.get('$_storeBase/categories');
        return (res.data as List)
            .map((e) => StoreCategory.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  // ---------- departments ----------
  Future<List<StoreDepartment>> storeDepartments({bool includeInactive = true}) =>
      client.guard(() async {
        final res = await client.dio.get('$_storeBase/departments',
            queryParameters: {'includeInactive': includeInactive});
        return (res.data as List)
            .map((e) => StoreDepartment.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  // ---------- suppliers ----------
  Future<List<StoreSupplier>> storeSuppliers() => client.guard(() async {
        final res = await client.dio.get('$_storeBase/suppliers');
        return (res.data as List)
            .map((e) => StoreSupplier.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  // ---------- goods receipts (GRN) ----------
  Future<Paged<StoreGoodsReceipt>> storeGoodsReceipts({
    String? status,
    int page = 1,
    int pageSize = 50,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_storeBase/goods-receipts', queryParameters: {
          if (status != null) 'status': status,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, StoreGoodsReceipt.fromJson);
      });

  // ---------- requisitions ----------
  Future<Paged<StoreRequisition>> storeRequisitions({
    String? status,
    String? departmentId,
    int page = 1,
    int pageSize = 50,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_storeBase/requisitions', queryParameters: {
          if (status != null) 'status': status,
          if (departmentId != null) 'departmentId': departmentId,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, StoreRequisition.fromJson);
      });

  // ---------- issues ----------
  Future<Paged<StoreIssue>> storeIssues({
    String? status,
    String? departmentId,
    int page = 1,
    int pageSize = 50,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_storeBase/issues', queryParameters: {
          if (status != null) 'status': status,
          if (departmentId != null) 'departmentId': departmentId,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, StoreIssue.fromJson);
      });

  // ---------- movement ledger ----------
  Future<StoreMovementLedger> storeLedger({
    String? itemId,
    DateTime? from,
    DateTime? to,
    int take = 1000,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_storeBase/ledger', queryParameters: {
          if (itemId != null) 'itemId': itemId,
          if (from != null) 'from': from.toUtc().toIso8601String(),
          if (to != null) 'to': to.toUtc().toIso8601String(),
          'take': take,
        });
        return StoreMovementLedger.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- supplier payables ----------
  Future<List<StoreSupplierPayable>> storePayables({bool outstandingOnly = false}) =>
      client.guard(() async {
        final res = await client.dio.get('$_storeBase/payables',
            queryParameters: {'outstandingOnly': outstandingOnly});
        return (res.data as List)
            .map((e) => StoreSupplierPayable.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  // ---------- department consumption report ----------
  Future<StoreDepartmentConsumption> storeDepartmentIssues({
    required DateTime from,
    required DateTime to,
    String? departmentId,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_storeBase/reports/department-issues', queryParameters: {
          'from': from.toUtc().toIso8601String(),
          'to': to.toUtc().toIso8601String(),
          if (departmentId != null) 'departmentId': departmentId,
        });
        return StoreDepartmentConsumption.fromJson(res.data as Map<String, dynamic>);
      });
}
