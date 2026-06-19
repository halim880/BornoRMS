import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import 'stock_api.dart';
import 'stock_models.dart';

// ---------- dashboard ----------
final stockDashboardProvider =
    FutureProvider<StockDashboard>((ref) => ref.read(staffApiProvider).stockDashboard());

// ---------- reference data (shared by create/edit dialogs) ----------
final stockCategoriesProvider =
    FutureProvider<List<StockCategory>>((ref) => ref.read(staffApiProvider).stockCategories());

final stockUnitsProvider =
    FutureProvider<List<StockUnit>>((ref) => ref.read(staffApiProvider).stockUnits());

// ---------- items (server-paged + filtered) ----------
class StockItemsFilter {
  final String? search;
  final bool lowStockOnly;
  final bool includeInactive;
  final int page;
  const StockItemsFilter({
    this.search,
    this.lowStockOnly = false,
    this.includeInactive = true,
    this.page = 1,
  });

  StockItemsFilter copyWith({String? search, bool? lowStockOnly, bool? includeInactive, int? page}) =>
      StockItemsFilter(
        search: search ?? this.search,
        lowStockOnly: lowStockOnly ?? this.lowStockOnly,
        includeInactive: includeInactive ?? this.includeInactive,
        page: page ?? this.page,
      );
}

final stockItemsFilterProvider =
    StateProvider<StockItemsFilter>((ref) => const StockItemsFilter());

final stockItemsProvider = FutureProvider<Paged<StockItem>>((ref) {
  final f = ref.watch(stockItemsFilterProvider);
  return ref.read(staffApiProvider).stockItems(
        search: f.search,
        lowStockOnly: f.lowStockOnly,
        includeInactive: f.includeInactive,
        page: f.page,
        pageSize: 25,
      );
});

/// All items (one big page) for picker dropdowns in wastage / adjust dialogs.
final stockAllItemsProvider = FutureProvider<List<StockItem>>((ref) async {
  final paged = await ref.read(staffApiProvider).stockItems(includeInactive: false, pageSize: 200);
  return paged.items;
});

// ---------- skus ----------
final stockSkusProvider =
    FutureProvider<List<ProductSkus>>((ref) => ref.read(staffApiProvider).stockSkus());

// ---------- low stock ----------
final stockLowStockProvider =
    FutureProvider<List<StockItem>>((ref) => ref.read(staffApiProvider).stockLowStock());

// ---------- recipes ----------
final stockRecipesProvider =
    FutureProvider<List<RecipeListRow>>((ref) => ref.read(staffApiProvider).stockRecipes());

final stockRecipeProvider = FutureProvider.family<Recipe?, String>(
    (ref, productId) => ref.read(staffApiProvider).stockRecipe(productId));

// ---------- suppliers ----------
final stockSuppliersProvider =
    FutureProvider<List<Supplier>>((ref) => ref.read(staffApiProvider).stockSuppliers());

// ---------- purchase orders ----------
final stockPoPageProvider = StateProvider<int>((ref) => 1);

final stockPurchaseOrdersProvider = FutureProvider<Paged<PurchaseOrderRow>>((ref) {
  final page = ref.watch(stockPoPageProvider);
  return ref.read(staffApiProvider).stockPurchaseOrders(page: page, pageSize: 25);
});

final stockPurchaseOrderProvider = FutureProvider.family<PurchaseOrderDetail, String>(
    (ref, id) => ref.read(staffApiProvider).stockPurchaseOrder(id));

// ---------- goods receipts ----------
final stockGrnPageProvider = StateProvider<int>((ref) => 1);

final stockGoodsReceiptsProvider = FutureProvider<Paged<GoodsReceiptRow>>((ref) {
  final page = ref.watch(stockGrnPageProvider);
  return ref.read(staffApiProvider).stockGoodsReceipts(page: page, pageSize: 25);
});

final stockGoodsReceiptProvider = FutureProvider.family<GoodsReceiptDetail, String>(
    (ref, id) => ref.read(staffApiProvider).stockGoodsReceipt(id));

// ---------- stock movements / history ----------
final stockMovementsPageProvider = StateProvider<int>((ref) => 1);

final stockMovementsProvider = FutureProvider<Paged<StockMovement>>((ref) {
  final page = ref.watch(stockMovementsPageProvider);
  return ref.read(staffApiProvider).stockMovements(page: page, pageSize: 25);
});
