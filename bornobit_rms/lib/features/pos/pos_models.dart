// POS-specific DTOs mirroring the backend (decimal→double, Guid→String, enums→String).

double _d(dynamic v) => v == null ? 0 : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();

class PosVariant {
  final String id;
  final String name;
  final double price;
  PosVariant({required this.id, required this.name, required this.price});

  factory PosVariant.fromJson(Map<String, dynamic> j) =>
      PosVariant(id: _s(j['id']), name: _s(j['name']), price: _d(j['price']));
}

class PosProduct {
  final String id;
  final String code;
  final String name;
  final String categoryId;
  final String categoryName;
  final double price;
  final String currency;
  final String? imagePath;
  final int displayOrder;
  final bool isActive;
  final bool isCombo;
  final int optionGroupCount;
  final List<PosVariant> variants;

  PosProduct({
    required this.id,
    required this.code,
    required this.name,
    required this.categoryId,
    required this.categoryName,
    required this.price,
    required this.currency,
    required this.imagePath,
    required this.displayOrder,
    required this.isActive,
    required this.isCombo,
    required this.optionGroupCount,
    required this.variants,
  });

  bool get hasVariants => variants.isNotEmpty;
  bool get hasOptions => optionGroupCount > 0;

  /// Lowest sell price (base, or cheapest variant) — used for the "from X" label.
  double get fromPrice =>
      hasVariants ? variants.map((v) => v.price).reduce((a, b) => a < b ? a : b) : price;

  factory PosProduct.fromJson(Map<String, dynamic> j) => PosProduct(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        categoryId: _s(j['productCategoryId']),
        categoryName: _s(j['categoryName']),
        price: _d(j['price']),
        currency: j['currency'] as String? ?? 'Tk',
        imagePath: _sOrNull(j['imagePath']),
        displayOrder: _i(j['displayOrder']),
        isActive: j['isActive'] == true,
        isCombo: j['isCombo'] == true,
        optionGroupCount: _i(j['optionGroupCount']),
        variants: (j['variants'] as List? ?? [])
            .map((e) => PosVariant.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class PosCategory {
  final String id;
  final String name;
  final int displayOrder;
  final bool isActive;
  PosCategory({required this.id, required this.name, required this.displayOrder, required this.isActive});

  factory PosCategory.fromJson(Map<String, dynamic> j) => PosCategory(
        id: _s(j['id']),
        name: _s(j['name']),
        displayOrder: _i(j['displayOrder']),
        isActive: j['isActive'] == true,
      );
}

class PosAvailability {
  final String productId;
  final double availableStock;
  final bool isLowStock;
  final bool isOutOfStock;
  PosAvailability({required this.productId, required this.availableStock, required this.isLowStock, required this.isOutOfStock});

  factory PosAvailability.fromJson(Map<String, dynamic> j) => PosAvailability(
        productId: _s(j['productId']),
        availableStock: _d(j['availableStock']),
        isLowStock: j['isLowStock'] == true,
        isOutOfStock: j['isOutOfStock'] == true,
      );
}

class PosOption {
  final String id;
  final String name;
  final double priceDelta;
  PosOption({required this.id, required this.name, required this.priceDelta});

  factory PosOption.fromJson(Map<String, dynamic> j) =>
      PosOption(id: _s(j['id']), name: _s(j['name']), priceDelta: _d(j['priceDelta']));
}

class PosOptionGroup {
  final String id;
  final String name;
  final int minSelections;
  final int maxSelections;
  final List<PosOption> options;
  PosOptionGroup({required this.id, required this.name, required this.minSelections, required this.maxSelections, required this.options});

  bool get required => minSelections > 0;
  bool get single => maxSelections == 1;

  factory PosOptionGroup.fromJson(Map<String, dynamic> j) => PosOptionGroup(
        id: _s(j['id']),
        name: _s(j['name']),
        minSelections: _i(j['minSelections']),
        maxSelections: _i(j['maxSelections']),
        options: (j['options'] as List? ?? [])
            .map((e) => PosOption.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class PosTable {
  final String id;
  final String tableNumber;
  final int capacity;
  PosTable({required this.id, required this.tableNumber, required this.capacity});

  factory PosTable.fromJson(Map<String, dynamic> j) =>
      PosTable(id: _s(j['id']), tableNumber: _s(j['tableNumber']), capacity: _i(j['capacity']));
}

class ActiveOrder {
  final String id;
  final String orderNumber;
  final String orderType;
  final String? tableId;
  final String? tableNumber;
  final String status;
  final int itemCount;
  final double total;
  final String currency;

  ActiveOrder({
    required this.id,
    required this.orderNumber,
    required this.orderType,
    required this.tableId,
    required this.tableNumber,
    required this.status,
    required this.itemCount,
    required this.total,
    required this.currency,
  });

  factory ActiveOrder.fromJson(Map<String, dynamic> j) => ActiveOrder(
        id: _s(j['id']),
        orderNumber: _s(j['orderNumber']),
        orderType: _s(j['orderType']),
        tableId: _sOrNull(j['tableId']),
        tableNumber: _sOrNull(j['tableNumber']),
        status: _s(j['status']),
        itemCount: _i(j['itemCount']),
        total: _d(j['total']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

class PlaceOrderResult {
  final String orderId;
  final String orderNumber;
  final double total;
  final String currency;
  PlaceOrderResult({required this.orderId, required this.orderNumber, required this.total, required this.currency});

  factory PlaceOrderResult.fromJson(Map<String, dynamic> j) => PlaceOrderResult(
        orderId: _s(j['orderId']),
        orderNumber: _s(j['orderNumber']),
        total: _d(j['total']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

class BillSummary {
  final double subtotal;
  final double discountAmount;
  final double grandTotal;
  final double rounding;
  final bool isPaid;
  BillSummary({required this.subtotal, required this.discountAmount, required this.grandTotal, required this.rounding, required this.isPaid});

  factory BillSummary.fromJson(Map<String, dynamic> j) => BillSummary(
        subtotal: _d(j['subtotal']),
        discountAmount: _d(j['discountAmount']),
        grandTotal: _d(j['grandTotal']),
        rounding: _d(j['rounding']),
        isPaid: j['isPaid'] == true,
      );
}

class SettlementResult {
  final String orderId;
  final double grandTotal;
  final double amountPaid;
  final double balanceDue;
  final String paymentStatus; // NotPaid | PartiallyPaid | Paid | ...
  final double change;
  final List<String> warnings;

  SettlementResult({
    required this.orderId,
    required this.grandTotal,
    required this.amountPaid,
    required this.balanceDue,
    required this.paymentStatus,
    required this.change,
    required this.warnings,
  });

  bool get isPaid => paymentStatus == 'Paid';

  factory SettlementResult.fromJson(Map<String, dynamic> j) => SettlementResult(
        orderId: _s(j['orderId']),
        grandTotal: _d(j['grandTotal']),
        amountPaid: _d(j['amountPaid']),
        balanceDue: _d(j['balanceDue']),
        paymentStatus: _s(j['paymentStatus']),
        change: _d(j['change']),
        warnings: (j['warnings'] as List? ?? []).map((e) => e.toString()).toList(),
      );
}

// ---------- cash drawer / shift ----------

class CashDrawer {
  final String id;
  final String drawerNumber;
  final String cashierName;
  final String? cashAccountName;
  final double openingBalance;
  final double cashReceived;
  final double cashPaidOut;
  final double expectedClosingBalance;
  final double? countedClosingBalance;
  final double variance;
  final String status; // Open | Closing | Closed
  final DateTime? openedAtUtc;

  CashDrawer({
    required this.id,
    required this.drawerNumber,
    required this.cashierName,
    required this.cashAccountName,
    required this.openingBalance,
    required this.cashReceived,
    required this.cashPaidOut,
    required this.expectedClosingBalance,
    required this.countedClosingBalance,
    required this.variance,
    required this.status,
    required this.openedAtUtc,
  });

  bool get isOpen => status == 'Open';

  factory CashDrawer.fromJson(Map<String, dynamic> j) => CashDrawer(
        id: _s(j['id']),
        drawerNumber: _s(j['drawerNumber']),
        cashierName: _s(j['cashierName']),
        cashAccountName: _sOrNull(j['cashAccountName']),
        openingBalance: _d(j['openingBalance']),
        cashReceived: _d(j['cashReceived']),
        cashPaidOut: _d(j['cashPaidOut']),
        expectedClosingBalance: _d(j['expectedClosingBalance']),
        countedClosingBalance:
            j['countedClosingBalance'] == null ? null : _d(j['countedClosingBalance']),
        variance: _d(j['variance']),
        status: _s(j['status']),
        openedAtUtc: j['openedAtUtc'] == null ? null : DateTime.tryParse(j['openedAtUtc'].toString()),
      );
}

class DrawerCloseResult {
  final String drawerNumber;
  final double expected;
  final double counted;
  final double variance;
  DrawerCloseResult({required this.drawerNumber, required this.expected, required this.counted, required this.variance});

  factory DrawerCloseResult.fromJson(Map<String, dynamic> j) => DrawerCloseResult(
        drawerNumber: _s(j['drawerNumber']),
        expected: _d(j['expected']),
        counted: _d(j['counted']),
        variance: _d(j['variance']),
      );
}

class DrawerMethodLine {
  final String method;
  final int count;
  final double amount;
  DrawerMethodLine({required this.method, required this.count, required this.amount});

  factory DrawerMethodLine.fromJson(Map<String, dynamic> j) =>
      DrawerMethodLine(method: _s(j['method']), count: _i(j['count']), amount: _d(j['amount']));
}

class DrawerSummary {
  final CashDrawer drawer;
  final List<DrawerMethodLine> byMethod;
  DrawerSummary({required this.drawer, required this.byMethod});

  factory DrawerSummary.fromJson(Map<String, dynamic> j) => DrawerSummary(
        drawer: CashDrawer.fromJson(j['drawer'] as Map<String, dynamic>),
        byMethod: (j['byMethod'] as List? ?? [])
            .map((e) => DrawerMethodLine.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

// ---------- cart staging (client-side, before SetPosOrderLines) ----------

/// A staged cart line keyed by product + variant + chosen options.
class CartLine {
  final String menuItemId;
  final String? variantId;
  final List<String> optionIds;
  final String name; // display: product (+ variant)
  final double unitPrice; // base/variant price + option deltas
  int quantity;
  final List<String> optionLabels;

  CartLine({
    required this.menuItemId,
    required this.variantId,
    required this.optionIds,
    required this.name,
    required this.unitPrice,
    required this.quantity,
    required this.optionLabels,
  });

  double get lineTotal => unitPrice * quantity;

  /// Identity key — duplicates with the same product/variant/options merge.
  String get key {
    final opt = (optionIds.toList()..sort()).join(',');
    return '$menuItemId|${variantId ?? ''}|$opt';
  }
}
