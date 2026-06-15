// Hand-rolled DTOs mirroring the Application-layer records the /waiter endpoints
// return. The API uses System.Text.Json camelCase keys. Rules:
//   - Guid  -> String (kept as String; never parsed; compared by equality)
//   - decimal -> double (DISPLAY ONLY — never re-sum money client-side; the
//     authoritative totals come back from the server)
//   - enums -> String parsed via enums.dart with an `unknown` fallback
//   - DateTime -> parsed to UTC (most "minutes" values are precomputed server-side)

import 'enums.dart';

double _d(dynamic v) => v == null ? 0 : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
int? _in(dynamic v) => v == null ? null : (v as num).toInt();

class WaiterDashboard {
  final int myTables, availableTables, occupiedTables, pendingRequests;
  final int readyToServeOrders, billsWaiting, myActiveSessions;
  final double myRevenueServedToday;
  final String currency;

  WaiterDashboard({
    required this.myTables,
    required this.availableTables,
    required this.occupiedTables,
    required this.pendingRequests,
    required this.readyToServeOrders,
    required this.billsWaiting,
    required this.myActiveSessions,
    required this.myRevenueServedToday,
    required this.currency,
  });

  factory WaiterDashboard.fromJson(Map<String, dynamic> j) => WaiterDashboard(
        myTables: _i(j['myTables']),
        availableTables: _i(j['availableTables']),
        occupiedTables: _i(j['occupiedTables']),
        pendingRequests: _i(j['pendingRequests']),
        readyToServeOrders: _i(j['readyToServeOrders']),
        billsWaiting: _i(j['billsWaiting']),
        myActiveSessions: _i(j['myActiveSessions']),
        myRevenueServedToday: _d(j['myRevenueServedToday']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

class TableOverviewRow {
  final String tableId;
  final String tableNumber;
  final int capacity;
  final DerivedTableStatus status;
  final int? guestCount;
  final int? sessionMinutes;
  final double currentBill;
  final String? orderId;
  final String? orderNumber;
  final String currency;
  final String? sessionId;
  final int orderCount;
  final String? waiterName;

  TableOverviewRow({
    required this.tableId,
    required this.tableNumber,
    required this.capacity,
    required this.status,
    required this.guestCount,
    required this.sessionMinutes,
    required this.currentBill,
    required this.orderId,
    required this.orderNumber,
    required this.currency,
    required this.sessionId,
    required this.orderCount,
    required this.waiterName,
  });

  factory TableOverviewRow.fromJson(Map<String, dynamic> j) => TableOverviewRow(
        tableId: j['tableId'] as String,
        tableNumber: j['tableNumber'] as String,
        capacity: _i(j['capacity']),
        status: DerivedTableStatus.fromName(j['status'] as String?),
        guestCount: _in(j['guestCount']),
        sessionMinutes: _in(j['sessionMinutes']),
        currentBill: _d(j['currentBill']),
        orderId: j['orderId'] as String?,
        orderNumber: j['orderNumber'] as String?,
        currency: j['currency'] as String? ?? 'Tk',
        sessionId: j['sessionId'] as String?,
        orderCount: _i(j['orderCount']),
        waiterName: j['waiterName'] as String?,
      );
}

class ReadyToServeLine {
  final String name;
  final int quantity;
  final String? stationName;
  ReadyToServeLine(this.name, this.quantity, this.stationName);
  factory ReadyToServeLine.fromJson(Map<String, dynamic> j) =>
      ReadyToServeLine(j['name'] as String, _i(j['quantity']), j['stationName'] as String?);
}

class ReadyToServeRow {
  final String orderId;
  final String orderNumber;
  final String? tableNumber;
  final int waitingMinutes;
  final List<ReadyToServeLine> items;

  ReadyToServeRow({
    required this.orderId,
    required this.orderNumber,
    required this.tableNumber,
    required this.waitingMinutes,
    required this.items,
  });

  factory ReadyToServeRow.fromJson(Map<String, dynamic> j) => ReadyToServeRow(
        orderId: j['orderId'] as String,
        orderNumber: j['orderNumber'] as String,
        tableNumber: j['tableNumber'] as String?,
        waitingMinutes: _i(j['waitingMinutes']),
        items: (j['items'] as List? ?? [])
            .map((e) => ReadyToServeLine.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class CustomerRequestRow {
  final String id;
  final String restaurantTableId;
  final String tableNumber;
  final CustomerRequestType type;
  final int waitingMinutes;
  final String? note;

  CustomerRequestRow({
    required this.id,
    required this.restaurantTableId,
    required this.tableNumber,
    required this.type,
    required this.waitingMinutes,
    required this.note,
  });

  factory CustomerRequestRow.fromJson(Map<String, dynamic> j) => CustomerRequestRow(
        id: j['id'] as String,
        restaurantTableId: j['restaurantTableId'] as String,
        tableNumber: j['tableNumber'] as String,
        type: CustomerRequestType.fromName(j['type'] as String?),
        waitingMinutes: _i(j['waitingMinutes']),
        note: j['note'] as String?,
      );
}

class SessionRow {
  final String id;
  final String sessionNumber;
  final String tableNumber;
  final int guestCount;
  final String? waiterName;
  final DiningSessionStatus status;
  final int sessionMinutes;
  final int orderCount;
  final double runningBill;
  final String currency;

  SessionRow({
    required this.id,
    required this.sessionNumber,
    required this.tableNumber,
    required this.guestCount,
    required this.waiterName,
    required this.status,
    required this.sessionMinutes,
    required this.orderCount,
    required this.runningBill,
    required this.currency,
  });

  factory SessionRow.fromJson(Map<String, dynamic> j) => SessionRow(
        id: j['id'] as String,
        sessionNumber: j['sessionNumber'] as String,
        tableNumber: j['tableNumber'] as String? ?? '',
        guestCount: _i(j['guestCount']),
        waiterName: j['waiterName'] as String?,
        status: DiningSessionStatus.fromName(j['status'] as String?),
        sessionMinutes: _i(j['sessionMinutes']),
        orderCount: _i(j['orderCount']),
        runningBill: _d(j['runningBill']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

class SessionBillLine {
  final String name;
  final int quantity;
  final double unitPrice;
  final double lineTotal;
  SessionBillLine(this.name, this.quantity, this.unitPrice, this.lineTotal);
  factory SessionBillLine.fromJson(Map<String, dynamic> j) => SessionBillLine(
        j['name'] as String,
        _i(j['quantity']),
        _d(j['unitPrice']),
        _d(j['lineTotal']),
      );
}

class SessionBillOrder {
  final String orderId;
  final String orderNumber;
  final String status;
  final bool isPaid;
  final double orderTotal;
  final List<SessionBillLine> lines;
  SessionBillOrder({
    required this.orderId,
    required this.orderNumber,
    required this.status,
    required this.isPaid,
    required this.orderTotal,
    required this.lines,
  });
  factory SessionBillOrder.fromJson(Map<String, dynamic> j) => SessionBillOrder(
        orderId: j['orderId'] as String,
        orderNumber: j['orderNumber'] as String,
        status: j['status'] as String? ?? '',
        isPaid: j['isPaid'] as bool? ?? false,
        orderTotal: _d(j['orderTotal']),
        lines: (j['lines'] as List? ?? [])
            .map((e) => SessionBillLine.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class SessionBill {
  final String sessionId;
  final String sessionNumber;
  final String tableNumber;
  final int guestCount;
  final List<SessionBillOrder> orders;
  final double subtotal, discountAmount, taxAmount, serviceChargeAmount;
  final double grandTotal, paidAmount, balanceDue;
  final String currency;

  SessionBill({
    required this.sessionId,
    required this.sessionNumber,
    required this.tableNumber,
    required this.guestCount,
    required this.orders,
    required this.subtotal,
    required this.discountAmount,
    required this.taxAmount,
    required this.serviceChargeAmount,
    required this.grandTotal,
    required this.paidAmount,
    required this.balanceDue,
    required this.currency,
  });

  factory SessionBill.fromJson(Map<String, dynamic> j) => SessionBill(
        sessionId: j['sessionId'] as String,
        sessionNumber: j['sessionNumber'] as String,
        tableNumber: j['tableNumber'] as String? ?? '',
        guestCount: _i(j['guestCount']),
        orders: (j['orders'] as List? ?? [])
            .map((e) => SessionBillOrder.fromJson(e as Map<String, dynamic>))
            .toList(),
        subtotal: _d(j['subtotal']),
        discountAmount: _d(j['discountAmount']),
        taxAmount: _d(j['taxAmount']),
        serviceChargeAmount: _d(j['serviceChargeAmount']),
        grandTotal: _d(j['grandTotal']),
        paidAmount: _d(j['paidAmount']),
        balanceDue: _d(j['balanceDue']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

class ActiveOrder {
  final String id;
  final String orderNumber;
  final OrderType orderType;
  final String? tableNumber;
  final OrderStatus status;
  final int itemCount;
  final double total;
  final String currency;

  ActiveOrder({
    required this.id,
    required this.orderNumber,
    required this.orderType,
    required this.tableNumber,
    required this.status,
    required this.itemCount,
    required this.total,
    required this.currency,
  });

  factory ActiveOrder.fromJson(Map<String, dynamic> j) => ActiveOrder(
        id: j['id'] as String,
        orderNumber: j['orderNumber'] as String,
        orderType: OrderType.fromName(j['orderType'] as String?),
        tableNumber: j['tableNumber'] as String?,
        status: OrderStatus.fromName(j['status'] as String?),
        itemCount: _i(j['itemCount']),
        total: _d(j['total']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

class OrderLine {
  final String menuItemId;
  final String? variantId;
  final String code;
  final String name;
  final double unitPrice;
  final int quantity;
  final double lineTotal;
  final String? notes;

  OrderLine({
    required this.menuItemId,
    required this.variantId,
    required this.code,
    required this.name,
    required this.unitPrice,
    required this.quantity,
    required this.lineTotal,
    required this.notes,
  });

  factory OrderLine.fromJson(Map<String, dynamic> j) => OrderLine(
        menuItemId: j['menuItemId'] as String,
        variantId: j['variantId'] as String?,
        code: j['code'] as String? ?? '',
        name: j['name'] as String,
        unitPrice: _d(j['unitPrice']),
        quantity: _i(j['quantity']),
        lineTotal: _d(j['lineTotal']),
        notes: j['notes'] as String?,
      );
}

class OrderDetail {
  final String id;
  final String orderNumber;
  final String? tableNumber;
  final OrderType orderType;
  final OrderStatus status;
  final String currency;
  final String? notes;
  final double total;
  final String? diningSessionId;
  final List<OrderLine> lines;

  OrderDetail({
    required this.id,
    required this.orderNumber,
    required this.tableNumber,
    required this.orderType,
    required this.status,
    required this.currency,
    required this.notes,
    required this.total,
    required this.diningSessionId,
    required this.lines,
  });

  factory OrderDetail.fromJson(Map<String, dynamic> j) => OrderDetail(
        id: j['id'] as String,
        orderNumber: j['orderNumber'] as String,
        tableNumber: j['tableNumber'] as String?,
        orderType: OrderType.fromName(j['orderType'] as String?),
        status: OrderStatus.fromName(j['status'] as String?),
        currency: j['currency'] as String? ?? 'Tk',
        notes: j['notes'] as String?,
        total: _d(j['total']),
        diningSessionId: j['diningSessionId'] as String?,
        lines: (j['lines'] as List? ?? [])
            .map((e) => OrderLine.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class ProductVariant {
  final String id;
  final String name;
  final double price;
  final int displayOrder;
  ProductVariant(this.id, this.name, this.price, this.displayOrder);
  factory ProductVariant.fromJson(Map<String, dynamic> j) =>
      ProductVariant(j['id'] as String, j['name'] as String, _d(j['price']), _i(j['displayOrder']));
}

class Product {
  final String id;
  final String code;
  final String name;
  final String productCategoryId;
  final String categoryName;
  final double price;
  final String currency;
  final String? imagePath;
  final int displayOrder;
  final bool isActive;
  final List<ProductVariant> variants;

  Product({
    required this.id,
    required this.code,
    required this.name,
    required this.productCategoryId,
    required this.categoryName,
    required this.price,
    required this.currency,
    required this.imagePath,
    required this.displayOrder,
    required this.isActive,
    required this.variants,
  });

  bool get hasVariants => variants.isNotEmpty;
  double get minPrice =>
      hasVariants ? variants.map((v) => v.price).reduce((a, b) => a < b ? a : b) : price;

  factory Product.fromJson(Map<String, dynamic> j) => Product(
        id: j['id'] as String,
        code: j['code'] as String? ?? '',
        name: j['name'] as String,
        productCategoryId: j['productCategoryId'] as String,
        categoryName: j['categoryName'] as String? ?? '',
        price: _d(j['price']),
        currency: j['currency'] as String? ?? 'Tk',
        imagePath: j['imagePath'] as String?,
        displayOrder: _i(j['displayOrder']),
        isActive: j['isActive'] as bool? ?? true,
        variants: (j['variants'] as List? ?? [])
            .map((e) => ProductVariant.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class ProductCategory {
  final String id;
  final String name;
  final int displayOrder;
  final bool isActive;
  ProductCategory(this.id, this.name, this.displayOrder, this.isActive);
  factory ProductCategory.fromJson(Map<String, dynamic> j) => ProductCategory(
        j['id'] as String,
        j['name'] as String,
        _i(j['displayOrder']),
        j['isActive'] as bool? ?? true,
      );
}

class RestaurantTable {
  final String id;
  final String tableNumber;
  final int capacity;
  RestaurantTable(this.id, this.tableNumber, this.capacity);
  factory RestaurantTable.fromJson(Map<String, dynamic> j) =>
      RestaurantTable(j['id'] as String, j['tableNumber'] as String, _i(j['capacity']));
}

class ProductAvailability {
  final String productId;
  final double availableStock;
  final bool isLowStock;
  final bool isOutOfStock;
  ProductAvailability(this.productId, this.availableStock, this.isLowStock, this.isOutOfStock);
  factory ProductAvailability.fromJson(Map<String, dynamic> j) => ProductAvailability(
        j['productId'] as String,
        _d(j['availableStock']),
        j['isLowStock'] as bool? ?? false,
        j['isOutOfStock'] as bool? ?? false,
      );
}

class StaffUser {
  final String id;
  final String fullName;
  final bool isActive;
  final List<String> roles;
  StaffUser(this.id, this.fullName, this.isActive, this.roles);
  factory StaffUser.fromJson(Map<String, dynamic> j) => StaffUser(
        j['id'] as String,
        (j['fullName'] as String?)?.isNotEmpty == true
            ? j['fullName'] as String
            : (j['userName'] as String? ?? '—'),
        j['isActive'] as bool? ?? true,
        (j['roles'] as List? ?? []).map((e) => e.toString()).toList(),
      );
}

class OpenSessionResult {
  final String sessionId;
  final String sessionNumber;
  OpenSessionResult(this.sessionId, this.sessionNumber);
  factory OpenSessionResult.fromJson(Map<String, dynamic> j) =>
      OpenSessionResult(j['sessionId'] as String, j['sessionNumber'] as String);
}

class PlaceOrderResult {
  final String orderId;
  final String orderNumber;
  final double total;
  final String currency;
  PlaceOrderResult(this.orderId, this.orderNumber, this.total, this.currency);
  factory PlaceOrderResult.fromJson(Map<String, dynamic> j) => PlaceOrderResult(
        j['orderId'] as String,
        j['orderNumber'] as String,
        _d(j['total']),
        j['currency'] as String? ?? 'Tk',
      );
}

/// The aggregate `/waiter/console` payload (one poll instead of four).
class WaiterConsole {
  final WaiterDashboard dashboard;
  final List<TableOverviewRow> floor;
  final List<ReadyToServeRow> ready;
  final List<CustomerRequestRow> requests;
  WaiterConsole({
    required this.dashboard,
    required this.floor,
    required this.ready,
    required this.requests,
  });
  factory WaiterConsole.fromJson(Map<String, dynamic> j) => WaiterConsole(
        dashboard: WaiterDashboard.fromJson(j['dashboard'] as Map<String, dynamic>),
        floor: (j['floor'] as List? ?? [])
            .map((e) => TableOverviewRow.fromJson(e as Map<String, dynamic>))
            .toList(),
        ready: (j['ready'] as List? ?? [])
            .map((e) => ReadyToServeRow.fromJson(e as Map<String, dynamic>))
            .toList(),
        requests: (j['requests'] as List? ?? [])
            .map((e) => CustomerRequestRow.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class AuthUser {
  final String id;
  final String email;
  final String fullName;
  final List<String> roles;
  AuthUser({required this.id, required this.email, required this.fullName, required this.roles});

  factory AuthUser.fromJson(Map<String, dynamic> j) => AuthUser(
        id: j['id'] as String? ?? '',
        email: j['email'] as String? ?? '',
        fullName: j['fullName'] as String? ?? '',
        roles: (j['roles'] as List? ?? []).map((e) => e.toString()).toList(),
      );

  Map<String, dynamic> toJson() =>
      {'id': id, 'email': email, 'fullName': fullName, 'roles': roles};

  /// Mirrors the Web `CanCloseSession` policy used to gate the Close button.
  bool get canCloseSession => roles.any((r) =>
      r == 'SuperAdmin' || r == 'Admin' || r == 'Manager' || r == 'Cashier');
}
