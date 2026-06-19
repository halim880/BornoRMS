// DTOs for the Catalog admin screens (mirror the Blazor inventory pages:
// Products.razor, ProductCategories.razor, Tables.razor). JSON field names match
// the C# DTO property names (camelCase): ProductDto, ProductCategoryDto,
// TableAdminDto.

double _d(dynamic v) => v == null ? 0 : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();

/// A product variant row (cheapest variant drives the "from" price).
class CatalogVariant {
  final String id;
  final String name;
  final double price;
  final int displayOrder;

  CatalogVariant({
    required this.id,
    required this.name,
    required this.price,
    required this.displayOrder,
  });

  factory CatalogVariant.fromJson(Map<String, dynamic> j) => CatalogVariant(
        id: _s(j['id']),
        name: _s(j['name']),
        price: _d(j['price']),
        displayOrder: _i(j['displayOrder']),
      );
}

/// A product list row (mirrors ProductDto).
class CatalogProduct {
  final String id;
  final String code;
  final String name;
  final String? banglaName;
  final String productCategoryId;
  final String categoryName;
  final double price;
  final String currency;
  final String? description;
  final String? imagePath;
  final int displayOrder;
  final bool isActive;
  final List<CatalogVariant> variants;
  final bool isCombo;
  final int optionGroupCount;

  CatalogProduct({
    required this.id,
    required this.code,
    required this.name,
    required this.banglaName,
    required this.productCategoryId,
    required this.categoryName,
    required this.price,
    required this.currency,
    required this.description,
    required this.imagePath,
    required this.displayOrder,
    required this.isActive,
    required this.variants,
    required this.isCombo,
    required this.optionGroupCount,
  });

  bool get hasVariants => variants.isNotEmpty;
  bool get hasOptions => optionGroupCount > 0;

  /// Lowest sellable price — cheapest variant, or the base price when no variants.
  double get minPrice =>
      hasVariants ? variants.map((v) => v.price).reduce((a, b) => a < b ? a : b) : price;

  factory CatalogProduct.fromJson(Map<String, dynamic> j) => CatalogProduct(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        banglaName: _sOrNull(j['banglaName']),
        productCategoryId: _s(j['productCategoryId']),
        categoryName: _s(j['categoryName']),
        price: _d(j['price']),
        currency: j['currency'] as String? ?? 'Tk',
        description: _sOrNull(j['description']),
        imagePath: _sOrNull(j['imagePath']),
        displayOrder: _i(j['displayOrder']),
        isActive: j['isActive'] as bool? ?? false,
        variants: (j['variants'] as List? ?? [])
            .map((e) => CatalogVariant.fromJson(e as Map<String, dynamic>))
            .toList(),
        isCombo: j['isCombo'] as bool? ?? false,
        optionGroupCount: _i(j['optionGroupCount']),
      );
}

/// A product category list row (mirrors ProductCategoryDto).
class CatalogCategory {
  final String id;
  final String name;
  final String? description;
  final int displayOrder;
  final bool isActive;
  final double? taxRatePercent;

  CatalogCategory({
    required this.id,
    required this.name,
    required this.description,
    required this.displayOrder,
    required this.isActive,
    required this.taxRatePercent,
  });

  factory CatalogCategory.fromJson(Map<String, dynamic> j) => CatalogCategory(
        id: _s(j['id']),
        name: _s(j['name']),
        description: _sOrNull(j['description']),
        displayOrder: _i(j['displayOrder']),
        isActive: j['isActive'] as bool? ?? false,
        taxRatePercent: j['taxRatePercent'] == null ? null : _d(j['taxRatePercent']),
      );
}

/// A dining table admin row (mirrors TableAdminDto).
class CatalogTable {
  final String id;
  final String tableNumber;
  final int capacity;
  final bool isActive;

  CatalogTable({
    required this.id,
    required this.tableNumber,
    required this.capacity,
    required this.isActive,
  });

  factory CatalogTable.fromJson(Map<String, dynamic> j) => CatalogTable(
        id: _s(j['id']),
        tableNumber: _s(j['tableNumber']),
        capacity: _i(j['capacity']),
        isActive: j['isActive'] as bool? ?? false,
      );
}
