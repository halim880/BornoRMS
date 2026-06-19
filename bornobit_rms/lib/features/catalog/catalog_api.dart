import '../../core/api/staff_api.dart';
import '../../core/config/app_config.dart';
import 'catalog_models.dart';

/// Catalog admin HTTP surface — the versioned `/api/v1/staff/catalog/*` routes
/// (CatalogAdminEndpoints.cs). Lives as an extension so the feature stays
/// self-contained.
extension CatalogApi on StaffApi {
  String get _base => '${AppConfig.apiPrefix}/staff/catalog';

  // ---------- products ----------
  Future<List<CatalogProduct>> catalogProducts() => client.guard(() async {
        final res = await client.dio.get('$_base/products');
        return (res.data as List)
            .map((e) => CatalogProduct.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<void> catalogCreateProduct({
    required String productCategoryId,
    required String code,
    required String name,
    String? banglaName,
    required double price,
    String? description,
    String? imagePath,
    required int displayOrder,
  }) =>
      client.guard(() async {
        await client.dio.post('$_base/products', data: {
          'productCategoryId': productCategoryId,
          'code': code,
          'name': name,
          if (banglaName != null) 'banglaName': banglaName,
          'price': price,
          if (description != null) 'description': description,
          if (imagePath != null) 'imagePath': imagePath,
          'displayOrder': displayOrder,
        });
      });

  Future<void> catalogUpdateProduct(
    String id, {
    required String productCategoryId,
    required String code,
    required String name,
    String? banglaName,
    required double price,
    String? description,
    String? imagePath,
    required int displayOrder,
  }) =>
      client.guard(() async {
        await client.dio.patch('$_base/products/$id', data: {
          'productCategoryId': productCategoryId,
          'code': code,
          'name': name,
          if (banglaName != null) 'banglaName': banglaName,
          'price': price,
          if (description != null) 'description': description,
          if (imagePath != null) 'imagePath': imagePath,
          'displayOrder': displayOrder,
        });
      });

  Future<void> catalogSetProductActive(String id, bool isActive) =>
      client.guard(() async {
        await client.dio.post('$_base/products/$id/active', data: {'isActive': isActive});
      });

  // ---------- categories ----------
  Future<List<CatalogCategory>> catalogCategories() => client.guard(() async {
        final res = await client.dio.get('$_base/categories');
        return (res.data as List)
            .map((e) => CatalogCategory.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<void> catalogCreateCategory({
    required String name,
    String? description,
    required int displayOrder,
    double? taxRatePercent,
  }) =>
      client.guard(() async {
        await client.dio.post('$_base/categories', data: {
          'name': name,
          if (description != null) 'description': description,
          'displayOrder': displayOrder,
          if (taxRatePercent != null) 'taxRatePercent': taxRatePercent,
        });
      });

  Future<void> catalogUpdateCategory(
    String id, {
    required String name,
    String? description,
    required int displayOrder,
    double? taxRatePercent,
  }) =>
      client.guard(() async {
        await client.dio.patch('$_base/categories/$id', data: {
          'name': name,
          if (description != null) 'description': description,
          'displayOrder': displayOrder,
          if (taxRatePercent != null) 'taxRatePercent': taxRatePercent,
        });
      });

  Future<void> catalogSetCategoryActive(String id, bool isActive) =>
      client.guard(() async {
        await client.dio.post('$_base/categories/$id/active', data: {'isActive': isActive});
      });

  // ---------- tables ----------
  Future<List<CatalogTable>> catalogTables() => client.guard(() async {
        final res = await client.dio.get('$_base/tables');
        return (res.data as List)
            .map((e) => CatalogTable.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<void> catalogCreateTable({required String tableNumber, required int capacity}) =>
      client.guard(() async {
        await client.dio.post('$_base/tables', data: {
          'tableNumber': tableNumber,
          'capacity': capacity,
        });
      });

  Future<void> catalogUpdateTable(String id,
          {required String tableNumber, required int capacity}) =>
      client.guard(() async {
        await client.dio.patch('$_base/tables/$id', data: {
          'tableNumber': tableNumber,
          'capacity': capacity,
        });
      });

  Future<void> catalogSetTableActive(String id, bool isActive) =>
      client.guard(() async {
        await client.dio.post('$_base/tables/$id/active', data: {'isActive': isActive});
      });
}
