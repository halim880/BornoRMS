// DTOs for the Stock / Inventory module (mirror the Blazor Stock pages and the
// C# records in BornoBit.Restaurant.Application.Inventory.*). JSON field names
// match the C# DTO property names (camelCase).

// ---- shared parse helpers (copied from the feature convention) ----
double _d(dynamic v) => v == null ? 0.0 : (v as num).toDouble();
double? _dOrNull(dynamic v) => v == null ? null : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();
DateTime _dt(dynamic v) => v == null ? DateTime.now() : DateTime.parse(v as String).toLocal();
DateTime? _dtOrNull(dynamic v) => v == null ? null : DateTime.parse(v as String).toLocal();
bool _b(dynamic v) => v as bool? ?? false;

// ---------- reference data ----------

/// A stock unit of measure (mirrors UnitDto).
class StockUnit {
  final String id;
  final String code;
  final String name;
  final bool isActive;

  StockUnit({required this.id, required this.code, required this.name, required this.isActive});

  factory StockUnit.fromJson(Map<String, dynamic> j) => StockUnit(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        isActive: _b(j['isActive']),
      );
}

/// An inventory category (mirrors InventoryCategoryDto).
class StockCategory {
  final String id;
  final String name;
  final int displayOrder;
  final bool isActive;

  StockCategory({required this.id, required this.name, required this.displayOrder, required this.isActive});

  factory StockCategory.fromJson(Map<String, dynamic> j) => StockCategory(
        id: _s(j['id']),
        name: _s(j['name']),
        displayOrder: _i(j['displayOrder']),
        isActive: _b(j['isActive']),
      );
}

// ---------- items ----------

/// A stock item row (mirrors InventoryItemDto). itemType: 1=Ingredient, 2=FinishedGood.
class StockItem {
  final String id;
  final String code;
  final String name;
  final String? banglaName;
  final String inventoryCategoryId;
  final String categoryName;
  final int itemType;
  final String baseUnitId;
  final String unitCode;
  final double qtyOnHand;
  final double reorderLevel;
  final double reorderQty;
  final double avgCost;
  final String currency;
  final bool isPerishable;
  final bool isActive;
  final String? productId;
  final String? variantId;
  final double? packSize;
  final String? packNote;
  final bool isLowStock;
  final double stockValue;

  StockItem({
    required this.id,
    required this.code,
    required this.name,
    required this.banglaName,
    required this.inventoryCategoryId,
    required this.categoryName,
    required this.itemType,
    required this.baseUnitId,
    required this.unitCode,
    required this.qtyOnHand,
    required this.reorderLevel,
    required this.reorderQty,
    required this.avgCost,
    required this.currency,
    required this.isPerishable,
    required this.isActive,
    required this.productId,
    required this.variantId,
    required this.packSize,
    required this.packNote,
    required this.isLowStock,
    required this.stockValue,
  });

  String get itemTypeLabel => itemType == 2 ? 'Finished Good' : 'Ingredient';

  factory StockItem.fromJson(Map<String, dynamic> j) => StockItem(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        banglaName: _sOrNull(j['banglaName']),
        inventoryCategoryId: _s(j['inventoryCategoryId']),
        categoryName: _s(j['categoryName']),
        itemType: _i(j['itemType']),
        baseUnitId: _s(j['baseUnitId']),
        unitCode: _s(j['unitCode']),
        qtyOnHand: _d(j['qtyOnHand']),
        reorderLevel: _d(j['reorderLevel']),
        reorderQty: _d(j['reorderQty']),
        avgCost: _d(j['avgCost']),
        currency: j['currency'] as String? ?? 'Tk',
        isPerishable: _b(j['isPerishable']),
        isActive: _b(j['isActive']),
        productId: _sOrNull(j['productId']),
        variantId: _sOrNull(j['variantId']),
        packSize: _dOrNull(j['packSize']),
        packNote: _sOrNull(j['packNote']),
        isLowStock: _b(j['isLowStock']),
        stockValue: _d(j['stockValue']),
      );
}

/// Aggregate totals (mirrors InventoryStockSummaryDto).
class StockSummary {
  final int itemCount;
  final double totalStockValue;
  final int lowStockCount;

  StockSummary({required this.itemCount, required this.totalStockValue, required this.lowStockCount});

  factory StockSummary.fromJson(Map<String, dynamic> j) => StockSummary(
        itemCount: _i(j['itemCount']),
        totalStockValue: _d(j['totalStockValue']),
        lowStockCount: _i(j['lowStockCount']),
      );
}

// ---------- dashboard ----------

class CategoryValue {
  final String categoryId;
  final String categoryName;
  final double value;
  CategoryValue({required this.categoryId, required this.categoryName, required this.value});
  factory CategoryValue.fromJson(Map<String, dynamic> j) => CategoryValue(
        categoryId: _s(j['categoryId']),
        categoryName: _s(j['categoryName']),
        value: _d(j['value']),
      );
}

class StockValuation {
  final double totalValue;
  final List<CategoryValue> byCategory;
  StockValuation({required this.totalValue, required this.byCategory});
  factory StockValuation.fromJson(Map<String, dynamic> j) => StockValuation(
        totalValue: _d(j['totalValue']),
        byCategory: ((j['byCategory'] as List?) ?? [])
            .map((e) => CategoryValue.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class OutOfStockRow {
  final String itemId;
  final String code;
  final String name;
  final String unitCode;
  final double currentStock;
  OutOfStockRow({
    required this.itemId,
    required this.code,
    required this.name,
    required this.unitCode,
    required this.currentStock,
  });
  factory OutOfStockRow.fromJson(Map<String, dynamic> j) => OutOfStockRow(
        itemId: _s(j['itemId']),
        code: _s(j['code']),
        name: _s(j['name']),
        unitCode: _s(j['unitCode']),
        currentStock: _d(j['currentStock']),
      );
}

class WasteRow {
  final String itemId;
  final String code;
  final String name;
  final double wasted;
  final double consumed;
  final double wastePercent;
  WasteRow({
    required this.itemId,
    required this.code,
    required this.name,
    required this.wasted,
    required this.consumed,
    required this.wastePercent,
  });
  factory WasteRow.fromJson(Map<String, dynamic> j) => WasteRow(
        itemId: _s(j['itemId']),
        code: _s(j['code']),
        name: _s(j['name']),
        wasted: _d(j['wasted']),
        consumed: _d(j['consumed']),
        wastePercent: _d(j['wastePercent']),
      );
}

class WastePercent {
  final double overallPercent;
  final double totalWasted;
  final double totalConsumed;
  final List<WasteRow> byItem;
  WastePercent({
    required this.overallPercent,
    required this.totalWasted,
    required this.totalConsumed,
    required this.byItem,
  });
  factory WastePercent.fromJson(Map<String, dynamic> j) => WastePercent(
        overallPercent: _d(j['overallPercent']),
        totalWasted: _d(j['totalWasted']),
        totalConsumed: _d(j['totalConsumed']),
        byItem: ((j['byItem'] as List?) ?? [])
            .map((e) => WasteRow.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class MoverRow {
  final String itemId;
  final String code;
  final String name;
  final String unitCode;
  final double qtyConsumed;
  MoverRow({
    required this.itemId,
    required this.code,
    required this.name,
    required this.unitCode,
    required this.qtyConsumed,
  });
  factory MoverRow.fromJson(Map<String, dynamic> j) => MoverRow(
        itemId: _s(j['itemId']),
        code: _s(j['code']),
        name: _s(j['name']),
        unitCode: _s(j['unitCode']),
        qtyConsumed: _d(j['qtyConsumed']),
      );
}

class FastSlowMovers {
  final List<MoverRow> fast;
  final List<MoverRow> slow;
  FastSlowMovers({required this.fast, required this.slow});
  factory FastSlowMovers.fromJson(Map<String, dynamic> j) => FastSlowMovers(
        fast: ((j['fast'] as List?) ?? []).map((e) => MoverRow.fromJson(e as Map<String, dynamic>)).toList(),
        slow: ((j['slow'] as List?) ?? []).map((e) => MoverRow.fromJson(e as Map<String, dynamic>)).toList(),
      );
}

class IngredientConsumption {
  final String itemId;
  final String code;
  final String name;
  final String unitCode;
  final double qtyConsumed;
  final double value;
  IngredientConsumption({
    required this.itemId,
    required this.code,
    required this.name,
    required this.unitCode,
    required this.qtyConsumed,
    required this.value,
  });
  factory IngredientConsumption.fromJson(Map<String, dynamic> j) => IngredientConsumption(
        itemId: _s(j['itemId']),
        code: _s(j['code']),
        name: _s(j['name']),
        unitCode: _s(j['unitCode']),
        qtyConsumed: _d(j['qtyConsumed']),
        value: _d(j['value']),
      );
}

/// The aggregate stock dashboard payload (one round-trip).
class StockDashboard {
  final StockSummary summary;
  final StockValuation valuation;
  final List<StockItem> lowStock;
  final List<OutOfStockRow> outOfStock;
  final WastePercent waste;
  final FastSlowMovers movers;
  final List<IngredientConsumption> consumption;

  StockDashboard({
    required this.summary,
    required this.valuation,
    required this.lowStock,
    required this.outOfStock,
    required this.waste,
    required this.movers,
    required this.consumption,
  });

  factory StockDashboard.fromJson(Map<String, dynamic> j) => StockDashboard(
        summary: StockSummary.fromJson(j['summary'] as Map<String, dynamic>),
        valuation: StockValuation.fromJson(j['valuation'] as Map<String, dynamic>),
        lowStock: ((j['lowStock'] as List?) ?? [])
            .map((e) => StockItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        outOfStock: ((j['outOfStock'] as List?) ?? [])
            .map((e) => OutOfStockRow.fromJson(e as Map<String, dynamic>))
            .toList(),
        waste: WastePercent.fromJson(j['waste'] as Map<String, dynamic>),
        movers: FastSlowMovers.fromJson(j['movers'] as Map<String, dynamic>),
        consumption: ((j['consumption'] as List?) ?? [])
            .map((e) => IngredientConsumption.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

// ---------- skus ----------

class SkuSlot {
  final String? variantId;
  final String? variantName;
  final String? itemId;
  final String? itemCode;
  final double? qtyOnHand;
  final String? unitCode;
  SkuSlot({
    required this.variantId,
    required this.variantName,
    required this.itemId,
    required this.itemCode,
    required this.qtyOnHand,
    required this.unitCode,
  });
  bool get hasSku => itemId != null;
  factory SkuSlot.fromJson(Map<String, dynamic> j) => SkuSlot(
        variantId: _sOrNull(j['variantId']),
        variantName: _sOrNull(j['variantName']),
        itemId: _sOrNull(j['itemId']),
        itemCode: _sOrNull(j['itemCode']),
        qtyOnHand: _dOrNull(j['qtyOnHand']),
        unitCode: _sOrNull(j['unitCode']),
      );
}

class ProductSkus {
  final String productId;
  final String code;
  final String name;
  final int method;
  final List<SkuSlot> slots;
  ProductSkus({
    required this.productId,
    required this.code,
    required this.name,
    required this.method,
    required this.slots,
  });
  int get covered => slots.where((s) => s.hasSku).length;
  factory ProductSkus.fromJson(Map<String, dynamic> j) => ProductSkus(
        productId: _s(j['productId']),
        code: _s(j['code']),
        name: _s(j['name']),
        method: _i(j['method']),
        slots: ((j['slots'] as List?) ?? []).map((e) => SkuSlot.fromJson(e as Map<String, dynamic>)).toList(),
      );
}

// ---------- recipes ----------

class RecipeListRow {
  final String productId;
  final String productCode;
  final String productName;
  final double yield;
  final int itemCount;
  final bool isActive;
  RecipeListRow({
    required this.productId,
    required this.productCode,
    required this.productName,
    required this.yield,
    required this.itemCount,
    required this.isActive,
  });
  factory RecipeListRow.fromJson(Map<String, dynamic> j) => RecipeListRow(
        productId: _s(j['productId']),
        productCode: _s(j['productCode']),
        productName: _s(j['productName']),
        yield: _d(j['yield']),
        itemCount: _i(j['itemCount']),
        isActive: _b(j['isActive']),
      );
}

class RecipeItem {
  final String id;
  final String inventoryItemId;
  final String itemCode;
  final String itemName;
  final double quantity;
  final String unitId;
  final String unitCode;
  RecipeItem({
    required this.id,
    required this.inventoryItemId,
    required this.itemCode,
    required this.itemName,
    required this.quantity,
    required this.unitId,
    required this.unitCode,
  });
  factory RecipeItem.fromJson(Map<String, dynamic> j) => RecipeItem(
        id: _s(j['id']),
        inventoryItemId: _s(j['inventoryItemId']),
        itemCode: _s(j['itemCode']),
        itemName: _s(j['itemName']),
        quantity: _d(j['quantity']),
        unitId: _s(j['unitId']),
        unitCode: _s(j['unitCode']),
      );
}

class Recipe {
  final String id;
  final String productId;
  final String productName;
  final String? variantId;
  final double yield;
  final bool isActive;
  final List<RecipeItem> items;
  Recipe({
    required this.id,
    required this.productId,
    required this.productName,
    required this.variantId,
    required this.yield,
    required this.isActive,
    required this.items,
  });
  factory Recipe.fromJson(Map<String, dynamic> j) => Recipe(
        id: _s(j['id']),
        productId: _s(j['productId']),
        productName: _s(j['productName']),
        variantId: _sOrNull(j['variantId']),
        yield: _d(j['yield']),
        isActive: _b(j['isActive']),
        items: ((j['items'] as List?) ?? []).map((e) => RecipeItem.fromJson(e as Map<String, dynamic>)).toList(),
      );
}

// ---------- suppliers ----------

class Supplier {
  final String id;
  final String code;
  final String name;
  final String? phone;
  final String? address;
  final int paymentTermsDays;
  final String? notes;
  final bool isActive;
  Supplier({
    required this.id,
    required this.code,
    required this.name,
    required this.phone,
    required this.address,
    required this.paymentTermsDays,
    required this.notes,
    required this.isActive,
  });
  factory Supplier.fromJson(Map<String, dynamic> j) => Supplier(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        phone: _sOrNull(j['phone']),
        address: _sOrNull(j['address']),
        paymentTermsDays: _i(j['paymentTermsDays']),
        notes: _sOrNull(j['notes']),
        isActive: _b(j['isActive']),
      );
}

// ---------- purchase orders ----------

/// PO status: 1=Draft, 2=Approved, 3=PartiallyReceived, 4=Received, 5=Cancelled.
class PurchaseOrderRow {
  final String id;
  final String poNumber;
  final String supplierId;
  final String supplierName;
  final DateTime orderedAtUtc;
  final DateTime? expectedAtUtc;
  final String currency;
  final int status;
  final int lineCount;
  final double subtotal;
  PurchaseOrderRow({
    required this.id,
    required this.poNumber,
    required this.supplierId,
    required this.supplierName,
    required this.orderedAtUtc,
    required this.expectedAtUtc,
    required this.currency,
    required this.status,
    required this.lineCount,
    required this.subtotal,
  });
  factory PurchaseOrderRow.fromJson(Map<String, dynamic> j) => PurchaseOrderRow(
        id: _s(j['id']),
        poNumber: _s(j['poNumber']),
        supplierId: _s(j['supplierId']),
        supplierName: _s(j['supplierName']),
        orderedAtUtc: _dt(j['orderedAtUtc']),
        expectedAtUtc: _dtOrNull(j['expectedAtUtc']),
        currency: j['currency'] as String? ?? 'Tk',
        status: _i(j['status']),
        lineCount: _i(j['lineCount']),
        subtotal: _d(j['subtotal']),
      );
}

class PurchaseOrderLine {
  final String id;
  final String inventoryItemId;
  final String itemName;
  final double qtyOrdered;
  final String unitCode;
  final double qtyOrderedBase;
  final double qtyReceivedBase;
  final double outstandingBase;
  final double unitCost;
  final double lineTotal;
  PurchaseOrderLine({
    required this.id,
    required this.inventoryItemId,
    required this.itemName,
    required this.qtyOrdered,
    required this.unitCode,
    required this.qtyOrderedBase,
    required this.qtyReceivedBase,
    required this.outstandingBase,
    required this.unitCost,
    required this.lineTotal,
  });
  factory PurchaseOrderLine.fromJson(Map<String, dynamic> j) => PurchaseOrderLine(
        id: _s(j['id']),
        inventoryItemId: _s(j['inventoryItemId']),
        itemName: _s(j['itemName']),
        qtyOrdered: _d(j['qtyOrdered']),
        unitCode: _s(j['unitCode']),
        qtyOrderedBase: _d(j['qtyOrderedBase']),
        qtyReceivedBase: _d(j['qtyReceivedBase']),
        outstandingBase: _d(j['outstandingBase']),
        unitCost: _d(j['unitCost']),
        lineTotal: _d(j['lineTotal']),
      );
}

class PurchaseOrderReceipt {
  final String id;
  final String grnNumber;
  final DateTime receivedAtUtc;
  final int status;
  final double subtotal;
  PurchaseOrderReceipt({
    required this.id,
    required this.grnNumber,
    required this.receivedAtUtc,
    required this.status,
    required this.subtotal,
  });
  factory PurchaseOrderReceipt.fromJson(Map<String, dynamic> j) => PurchaseOrderReceipt(
        id: _s(j['id']),
        grnNumber: _s(j['grnNumber']),
        receivedAtUtc: _dt(j['receivedAtUtc']),
        status: _i(j['status']),
        subtotal: _d(j['subtotal']),
      );
}

class PurchaseOrderDetail {
  final String id;
  final String poNumber;
  final String supplierId;
  final String supplierName;
  final DateTime orderedAtUtc;
  final DateTime? expectedAtUtc;
  final String currency;
  final String? notes;
  final int status;
  final DateTime? approvedAtUtc;
  final double subtotal;
  final List<PurchaseOrderLine> lines;
  final List<PurchaseOrderReceipt> receipts;
  PurchaseOrderDetail({
    required this.id,
    required this.poNumber,
    required this.supplierId,
    required this.supplierName,
    required this.orderedAtUtc,
    required this.expectedAtUtc,
    required this.currency,
    required this.notes,
    required this.status,
    required this.approvedAtUtc,
    required this.subtotal,
    required this.lines,
    required this.receipts,
  });
  factory PurchaseOrderDetail.fromJson(Map<String, dynamic> j) => PurchaseOrderDetail(
        id: _s(j['id']),
        poNumber: _s(j['poNumber']),
        supplierId: _s(j['supplierId']),
        supplierName: _s(j['supplierName']),
        orderedAtUtc: _dt(j['orderedAtUtc']),
        expectedAtUtc: _dtOrNull(j['expectedAtUtc']),
        currency: j['currency'] as String? ?? 'Tk',
        notes: _sOrNull(j['notes']),
        status: _i(j['status']),
        approvedAtUtc: _dtOrNull(j['approvedAtUtc']),
        subtotal: _d(j['subtotal']),
        lines: ((j['lines'] as List?) ?? [])
            .map((e) => PurchaseOrderLine.fromJson(e as Map<String, dynamic>))
            .toList(),
        receipts: ((j['receipts'] as List?) ?? [])
            .map((e) => PurchaseOrderReceipt.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

// ---------- goods receipts ----------

/// GRN status: 1=Draft, 2=Posted.
class GoodsReceiptRow {
  final String id;
  final String grnNumber;
  final String supplierId;
  final String supplierName;
  final String? invoiceNo;
  final DateTime receivedAtUtc;
  final String currency;
  final int status;
  final int lineCount;
  final double subtotal;
  GoodsReceiptRow({
    required this.id,
    required this.grnNumber,
    required this.supplierId,
    required this.supplierName,
    required this.invoiceNo,
    required this.receivedAtUtc,
    required this.currency,
    required this.status,
    required this.lineCount,
    required this.subtotal,
  });
  factory GoodsReceiptRow.fromJson(Map<String, dynamic> j) => GoodsReceiptRow(
        id: _s(j['id']),
        grnNumber: _s(j['grnNumber']),
        supplierId: _s(j['supplierId']),
        supplierName: _s(j['supplierName']),
        invoiceNo: _sOrNull(j['invoiceNo']),
        receivedAtUtc: _dt(j['receivedAtUtc']),
        currency: j['currency'] as String? ?? 'Tk',
        status: _i(j['status']),
        lineCount: _i(j['lineCount']),
        subtotal: _d(j['subtotal']),
      );
}

class GoodsReceiptLine {
  final String id;
  final String inventoryItemId;
  final String itemName;
  final double qty;
  final String unitCode;
  final double qtyBase;
  final double unitCost;
  final double lineTotal;
  GoodsReceiptLine({
    required this.id,
    required this.inventoryItemId,
    required this.itemName,
    required this.qty,
    required this.unitCode,
    required this.qtyBase,
    required this.unitCost,
    required this.lineTotal,
  });
  factory GoodsReceiptLine.fromJson(Map<String, dynamic> j) => GoodsReceiptLine(
        id: _s(j['id']),
        inventoryItemId: _s(j['inventoryItemId']),
        itemName: _s(j['itemName']),
        qty: _d(j['qty']),
        unitCode: _s(j['unitCode']),
        qtyBase: _d(j['qtyBase']),
        unitCost: _d(j['unitCost']),
        lineTotal: _d(j['lineTotal']),
      );
}

class GoodsReceiptDetail {
  final String id;
  final String grnNumber;
  final String supplierId;
  final String supplierName;
  final String? invoiceNo;
  final DateTime receivedAtUtc;
  final String currency;
  final String? notes;
  final int status;
  final DateTime? postedAtUtc;
  final double subtotal;
  final List<GoodsReceiptLine> lines;
  GoodsReceiptDetail({
    required this.id,
    required this.grnNumber,
    required this.supplierId,
    required this.supplierName,
    required this.invoiceNo,
    required this.receivedAtUtc,
    required this.currency,
    required this.notes,
    required this.status,
    required this.postedAtUtc,
    required this.subtotal,
    required this.lines,
  });
  factory GoodsReceiptDetail.fromJson(Map<String, dynamic> j) => GoodsReceiptDetail(
        id: _s(j['id']),
        grnNumber: _s(j['grnNumber']),
        supplierId: _s(j['supplierId']),
        supplierName: _s(j['supplierName']),
        invoiceNo: _sOrNull(j['invoiceNo']),
        receivedAtUtc: _dt(j['receivedAtUtc']),
        currency: j['currency'] as String? ?? 'Tk',
        notes: _sOrNull(j['notes']),
        status: _i(j['status']),
        postedAtUtc: _dtOrNull(j['postedAtUtc']),
        subtotal: _d(j['subtotal']),
        lines: ((j['lines'] as List?) ?? [])
            .map((e) => GoodsReceiptLine.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

// ---------- stock movements ----------

/// Movement type: 1=OpeningBalance, 2=PurchaseIn, 3=WastageOut, 4=AdjustmentIn,
/// 5=AdjustmentOut, 6=ConsumptionOut.
class StockMovement {
  final String id;
  final String inventoryItemId;
  final String itemCode;
  final String itemName;
  final String unitCode;
  final int movementType;
  final double qtyBase;
  final double unitCost;
  final String? reason;
  final String? referenceType;
  final DateTime occurredAtUtc;
  final String? createdBy;
  StockMovement({
    required this.id,
    required this.inventoryItemId,
    required this.itemCode,
    required this.itemName,
    required this.unitCode,
    required this.movementType,
    required this.qtyBase,
    required this.unitCost,
    required this.reason,
    required this.referenceType,
    required this.occurredAtUtc,
    required this.createdBy,
  });
  factory StockMovement.fromJson(Map<String, dynamic> j) => StockMovement(
        id: _s(j['id']),
        inventoryItemId: _s(j['inventoryItemId']),
        itemCode: _s(j['itemCode']),
        itemName: _s(j['itemName']),
        unitCode: _s(j['unitCode']),
        movementType: _i(j['movementType']),
        qtyBase: _d(j['qtyBase']),
        unitCost: _d(j['unitCost']),
        reason: _sOrNull(j['reason']),
        referenceType: _sOrNull(j['referenceType']),
        occurredAtUtc: _dt(j['occurredAtUtc']),
        createdBy: _sOrNull(j['createdBy']),
      );
}

// ---------- shared label/tone helpers ----------

String stockMovementLabel(int t) => switch (t) {
      1 => 'Opening',
      2 => 'Purchase In',
      3 => 'Wastage Out',
      4 => 'Adjustment In',
      5 => 'Adjustment Out',
      6 => 'Consumption',
      _ => 'Unknown',
    };

String stockMovementTone(int t) => switch (t) {
      1 => 'info',
      2 => 'success',
      3 => 'danger',
      4 => 'success',
      5 => 'warning',
      6 => 'neutral',
      _ => 'neutral',
    };

String poStatusLabel(int s) => switch (s) {
      1 => 'Draft',
      2 => 'Approved',
      3 => 'Partially Received',
      4 => 'Received',
      5 => 'Cancelled',
      _ => 'Unknown',
    };

String poStatusTone(int s) => switch (s) {
      1 => 'neutral',
      2 => 'info',
      3 => 'warning',
      4 => 'success',
      5 => 'danger',
      _ => 'neutral',
    };

String grnStatusLabel(int s) => switch (s) {
      1 => 'Draft',
      2 => 'Posted',
      _ => 'Unknown',
    };

String grnStatusTone(int s) => switch (s) {
      1 => 'neutral',
      2 => 'success',
      _ => 'neutral',
    };
