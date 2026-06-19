import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import 'catalog_api.dart';
import 'catalog_models.dart';

/// All products (active + inactive) for the admin grid. Refresh by
/// `ref.invalidate(catalogProductsProvider)` after a mutation.
final catalogProductsProvider = FutureProvider<List<CatalogProduct>>(
    (ref) => ref.read(staffApiProvider).catalogProducts());

/// All product categories. Also feeds the product create/edit category picker.
final catalogCategoriesProvider = FutureProvider<List<CatalogCategory>>(
    (ref) => ref.read(staffApiProvider).catalogCategories());

/// All dining tables (active + inactive).
final catalogTablesProvider = FutureProvider<List<CatalogTable>>(
    (ref) => ref.read(staffApiProvider).catalogTables());
