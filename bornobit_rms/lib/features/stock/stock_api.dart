import 'package:dio/dio.dart';

import '../../core/api/staff_api.dart';
import '../../core/config/app_config.dart';
import '../../core/models/dtos.dart';
import 'stock_models.dart';

/// Typed wrappers over the /api/v1/staff/stock/* surface (StockEndpoints.cs).
/// Lives as an extension on [StaffApi] so the Stock feature stays self-contained.
extension StockApi on StaffApi {
  String get _stockBase => '${AppConfig.apiPrefix}/staff/stock';

  // ---------- dashboard ----------
  Future<StockDashboard> stockDashboard({int days = 30}) => client.guard(() async {
        final res = await client.dio.get('$_stockBase/dashboard', queryParameters: {'days': days});
        return StockDashboard.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- items ----------
  Future<Paged<StockItem>> stockItems({
    String? search,
    String? categoryId,
    int? itemType,
    bool lowStockOnly = false,
    bool includeInactive = true,
    int page = 1,
    int pageSize = 25,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_stockBase/items', queryParameters: {
          if (search != null && search.isNotEmpty) 'search': search,
          if (categoryId != null) 'categoryId': categoryId,
          if (itemType != null) 'itemType': itemType,
          'lowStockOnly': lowStockOnly,
          'includeInactive': includeInactive,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, StockItem.fromJson);
      });

  Future<void> stockCreateItem(Map<String, dynamic> body) => client.guard(() async {
        await client.dio.post('$_stockBase/items', data: body);
      });

  Future<void> stockUpdateItem(String id, Map<String, dynamic> body) => client.guard(() async {
        await client.dio.put('$_stockBase/items/$id', data: body);
      });

  Future<void> stockSetItemActive(String id, bool isActive) => client.guard(() async {
        await client.dio.post('$_stockBase/items/$id/active', data: {'isActive': isActive});
      });

  // ---------- skus ----------
  Future<List<ProductSkus>> stockSkus() => client.guard(() async {
        final res = await client.dio.get('$_stockBase/skus');
        return (res.data as List).map((e) => ProductSkus.fromJson(e as Map<String, dynamic>)).toList();
      });

  Future<void> stockCreateSku(Map<String, dynamic> body) => client.guard(() async {
        await client.dio.post('$_stockBase/skus', data: body);
      });

  // ---------- low / out of stock ----------
  Future<List<StockItem>> stockLowStock() => client.guard(() async {
        final res = await client.dio.get('$_stockBase/low-stock');
        return (res.data as List).map((e) => StockItem.fromJson(e as Map<String, dynamic>)).toList();
      });

  // ---------- recipes ----------
  Future<List<RecipeListRow>> stockRecipes() => client.guard(() async {
        final res = await client.dio.get('$_stockBase/recipes');
        return (res.data as List).map((e) => RecipeListRow.fromJson(e as Map<String, dynamic>)).toList();
      });

  Future<Recipe?> stockRecipe(String productId) => client.guard(() async {
        final res = await client.dio.get(
          '$_stockBase/recipes/$productId',
          options: Options(validateStatus: (s) => s != null && (s < 300 || s == 404)),
        );
        if (res.statusCode == 404 || res.data is! Map) return null;
        return Recipe.fromJson(res.data as Map<String, dynamic>);
      });

  // ---------- suppliers ----------
  Future<List<Supplier>> stockSuppliers() => client.guard(() async {
        final res = await client.dio.get('$_stockBase/suppliers');
        return (res.data as List).map((e) => Supplier.fromJson(e as Map<String, dynamic>)).toList();
      });

  Future<void> stockCreateSupplier({
    required String code,
    required String name,
    String? phone,
    String? address,
    required int paymentTermsDays,
    String? notes,
  }) =>
      client.guard(() async {
        await client.dio.post('$_stockBase/suppliers', data: {
          'code': code,
          'name': name,
          if (phone != null) 'phone': phone,
          if (address != null) 'address': address,
          'paymentTermsDays': paymentTermsDays,
          if (notes != null) 'notes': notes,
        });
      });

  Future<void> stockUpdateSupplier(
    String id, {
    required String name,
    String? phone,
    String? address,
    required int paymentTermsDays,
    String? notes,
  }) =>
      client.guard(() async {
        await client.dio.put('$_stockBase/suppliers/$id', data: {
          'name': name,
          if (phone != null) 'phone': phone,
          if (address != null) 'address': address,
          'paymentTermsDays': paymentTermsDays,
          if (notes != null) 'notes': notes,
        });
      });

  Future<void> stockSetSupplierActive(String id, bool isActive) => client.guard(() async {
        await client.dio.post('$_stockBase/suppliers/$id/active', data: {'isActive': isActive});
      });

  // ---------- purchase orders (read-only) ----------
  Future<Paged<PurchaseOrderRow>> stockPurchaseOrders({int? status, int page = 1, int pageSize = 25}) =>
      client.guard(() async {
        final res = await client.dio.get('$_stockBase/purchase-orders', queryParameters: {
          if (status != null) 'status': status,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, PurchaseOrderRow.fromJson);
      });

  Future<PurchaseOrderDetail> stockPurchaseOrder(String id) => client.guard(() async {
        final res = await client.dio.get('$_stockBase/purchase-orders/$id');
        return PurchaseOrderDetail.fromJson(res.data as Map<String, dynamic>);
      });

  /// lines: each map = { itemId, qty, unitId, unitCost }. Returns the new PO id.
  Future<String> stockCreatePurchaseOrder({
    required String supplierId,
    DateTime? orderedAtUtc,
    DateTime? expectedAtUtc,
    String? notes,
    required List<Map<String, dynamic>> lines,
  }) =>
      client.guard(() async {
        final res = await client.dio.post('$_stockBase/purchase-orders', data: {
          'supplierId': supplierId,
          if (orderedAtUtc != null) 'orderedAtUtc': orderedAtUtc.toUtc().toIso8601String(),
          if (expectedAtUtc != null) 'expectedAtUtc': expectedAtUtc.toUtc().toIso8601String(),
          if (notes != null) 'notes': notes,
          'lines': lines,
        });
        return (res.data as Map<String, dynamic>)['id'].toString();
      });

  Future<void> stockUpdatePurchaseOrder(
    String id, {
    required String supplierId,
    DateTime? orderedAtUtc,
    DateTime? expectedAtUtc,
    String? notes,
    required List<Map<String, dynamic>> lines,
  }) =>
      client.guard(() async {
        await client.dio.put('$_stockBase/purchase-orders/$id', data: {
          'supplierId': supplierId,
          if (orderedAtUtc != null) 'orderedAtUtc': orderedAtUtc.toUtc().toIso8601String(),
          if (expectedAtUtc != null) 'expectedAtUtc': expectedAtUtc.toUtc().toIso8601String(),
          if (notes != null) 'notes': notes,
          'lines': lines,
        });
      });

  Future<void> stockApprovePurchaseOrder(String id) =>
      client.guard(() async => client.dio.post('$_stockBase/purchase-orders/$id/approve'));

  Future<void> stockCancelPurchaseOrder(String id) =>
      client.guard(() async => client.dio.post('$_stockBase/purchase-orders/$id/cancel'));

  Future<void> stockDeletePurchaseOrder(String id) =>
      client.guard(() async => client.dio.delete('$_stockBase/purchase-orders/$id'));

  // ---------- goods receipts (read-only) ----------
  Future<Paged<GoodsReceiptRow>> stockGoodsReceipts({int? status, int page = 1, int pageSize = 25}) =>
      client.guard(() async {
        final res = await client.dio.get('$_stockBase/goods-receipts', queryParameters: {
          if (status != null) 'status': status,
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, GoodsReceiptRow.fromJson);
      });

  Future<GoodsReceiptDetail> stockGoodsReceipt(String id) => client.guard(() async {
        final res = await client.dio.get('$_stockBase/goods-receipts/$id');
        return GoodsReceiptDetail.fromJson(res.data as Map<String, dynamic>);
      });

  /// lines: each map = { itemId, qty, unitId, unitCost, purchaseOrderLineId? }. Returns the new GRN id.
  Future<String> stockCreateGoodsReceipt({
    required String supplierId,
    String? invoiceNo,
    DateTime? receivedAtUtc,
    String? notes,
    required List<Map<String, dynamic>> lines,
    String? purchaseOrderId,
  }) =>
      client.guard(() async {
        final res = await client.dio.post('$_stockBase/goods-receipts', data: {
          'supplierId': supplierId,
          if (invoiceNo != null) 'invoiceNo': invoiceNo,
          if (receivedAtUtc != null) 'receivedAtUtc': receivedAtUtc.toUtc().toIso8601String(),
          if (notes != null) 'notes': notes,
          'lines': lines,
          if (purchaseOrderId != null) 'purchaseOrderId': purchaseOrderId,
        });
        return (res.data as Map<String, dynamic>)['id'].toString();
      });

  Future<void> stockUpdateGoodsReceipt(
    String id, {
    required String supplierId,
    String? invoiceNo,
    DateTime? receivedAtUtc,
    String? notes,
    required List<Map<String, dynamic>> lines,
  }) =>
      client.guard(() async {
        await client.dio.put('$_stockBase/goods-receipts/$id', data: {
          'supplierId': supplierId,
          if (invoiceNo != null) 'invoiceNo': invoiceNo,
          if (receivedAtUtc != null) 'receivedAtUtc': receivedAtUtc.toUtc().toIso8601String(),
          if (notes != null) 'notes': notes,
          'lines': lines,
        });
      });

  /// Posts the receipt → raises stock (moving-average cost) + Dr Purchases / Cr AP.
  /// Pass [paymentCashAccountId] to pay the supplier immediately from that cash account.
  Future<void> stockPostGoodsReceipt(String id, {String? paymentCashAccountId}) =>
      client.guard(() async {
        await client.dio.post('$_stockBase/goods-receipts/$id/post',
            data: {if (paymentCashAccountId != null) 'paymentCashAccountId': paymentCashAccountId});
      });

  Future<void> stockDeleteGoodsReceipt(String id) =>
      client.guard(() async => client.dio.delete('$_stockBase/goods-receipts/$id'));

  // ---------- purchase returns ----------
  /// Return goods to a supplier. lines: each map = { itemId, qty, unitId } (valued at current avg cost).
  /// Posts immediately (stock out + reduces the payable). Returns the new return id.
  Future<String> stockCreatePurchaseReturn({
    required String supplierId,
    DateTime? returnedAtUtc,
    String? reason,
    String? notes,
    required List<Map<String, dynamic>> lines,
  }) =>
      client.guard(() async {
        final res = await client.dio.post('$_stockBase/purchase-returns', data: {
          'supplierId': supplierId,
          if (returnedAtUtc != null) 'returnedAtUtc': returnedAtUtc.toUtc().toIso8601String(),
          if (reason != null) 'reason': reason,
          if (notes != null) 'notes': notes,
          'lines': lines,
        });
        return (res.data as Map<String, dynamic>)['id'].toString();
      });

  // ---------- wastage / adjustments ----------
  Future<void> stockRecordWastage({required String itemId, required double qtyBase, required String reason}) =>
      client.guard(() async {
        await client.dio.post('$_stockBase/wastage', data: {
          'itemId': itemId,
          'qtyBase': qtyBase,
          'reason': reason,
        });
      });

  Future<void> stockAdjust({required String itemId, required double countedQtyBase, String? reason}) =>
      client.guard(() async {
        await client.dio.post('$_stockBase/adjust', data: {
          'itemId': itemId,
          'countedQtyBase': countedQtyBase,
          if (reason != null) 'reason': reason,
        });
      });

  // ---------- stock movements / history ----------
  Future<Paged<StockMovement>> stockMovements({
    String? itemId,
    int? movementType,
    DateTime? fromUtc,
    DateTime? toUtc,
    int page = 1,
    int pageSize = 25,
  }) =>
      client.guard(() async {
        final res = await client.dio.get('$_stockBase/stock-movements', queryParameters: {
          if (itemId != null) 'itemId': itemId,
          if (movementType != null) 'movementType': movementType,
          if (fromUtc != null) 'fromUtc': fromUtc.toUtc().toIso8601String(),
          if (toUtc != null) 'toUtc': toUtc.toUtc().toIso8601String(),
          'page': page,
          'pageSize': pageSize,
        });
        return Paged.fromJson(res.data as Map<String, dynamic>, StockMovement.fromJson);
      });

  // ---------- reference data ----------
  Future<List<StockCategory>> stockCategories() => client.guard(() async {
        final res = await client.dio.get('$_stockBase/categories');
        return (res.data as List).map((e) => StockCategory.fromJson(e as Map<String, dynamic>)).toList();
      });

  Future<List<StockUnit>> stockUnits() => client.guard(() async {
        final res = await client.dio.get('$_stockBase/units');
        return (res.data as List).map((e) => StockUnit.fromJson(e as Map<String, dynamic>)).toList();
      });
}
