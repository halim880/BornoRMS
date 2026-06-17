// Dart mirrors of the backend DTOs (decimal→double, Guid→String, enums→String).
// Enums arrive as strings because the API registers JsonStringEnumConverter.

double _d(dynamic v) => v == null ? 0 : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
int? _iOrNull(dynamic v) => v == null ? null : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();

// ---------- auth ----------
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
}

// ---------- generic paging ----------
class Paged<T> {
  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;
  Paged({required this.items, required this.page, required this.pageSize, required this.totalCount, required this.totalPages});

  factory Paged.fromJson(Map<String, dynamic> j, T Function(Map<String, dynamic>) f) => Paged(
        items: (j['items'] as List? ?? []).map((e) => f(e as Map<String, dynamic>)).toList(),
        page: _i(j['page']),
        pageSize: _i(j['pageSize']),
        totalCount: _i(j['totalCount']),
        totalPages: _i(j['totalPages']),
      );

  static Paged<T> empty<T>() => Paged(items: const [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0);
}

// ---------- section 1: KPI summary ----------
class DashboardSummary {
  final double todayRevenue;
  final int todayOrderCount;
  final double averageOrderValue;
  final int occupiedTables;
  final int availableTables;
  final int reservedTables;
  final int waitingPaymentTables;
  final int pendingOrders;
  final int preparingOrders;
  final int readyOrders;
  final int activeDiningSessions;
  final int qrOrdersToday;
  final int walkInOrdersToday;
  final String currency;

  DashboardSummary({
    required this.todayRevenue,
    required this.todayOrderCount,
    required this.averageOrderValue,
    required this.occupiedTables,
    required this.availableTables,
    required this.reservedTables,
    required this.waitingPaymentTables,
    required this.pendingOrders,
    required this.preparingOrders,
    required this.readyOrders,
    required this.activeDiningSessions,
    required this.qrOrdersToday,
    required this.walkInOrdersToday,
    required this.currency,
  });

  factory DashboardSummary.fromJson(Map<String, dynamic> j) => DashboardSummary(
        todayRevenue: _d(j['todayRevenue']),
        todayOrderCount: _i(j['todayOrderCount']),
        averageOrderValue: _d(j['averageOrderValue']),
        occupiedTables: _i(j['occupiedTables']),
        availableTables: _i(j['availableTables']),
        reservedTables: _i(j['reservedTables']),
        waitingPaymentTables: _i(j['waitingPaymentTables']),
        pendingOrders: _i(j['pendingOrders']),
        preparingOrders: _i(j['preparingOrders']),
        readyOrders: _i(j['readyOrders']),
        activeDiningSessions: _i(j['activeDiningSessions']),
        qrOrdersToday: _i(j['qrOrdersToday']),
        walkInOrdersToday: _i(j['walkInOrdersToday']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

// ---------- section 2: live floor ----------
class TableOverviewRow {
  final String tableId;
  final String tableNumber;
  final int capacity;
  final String status; // Available | Occupied | Reserved | WaitingPayment
  final int? guestCount;
  final int? sessionMinutes;
  final double currentBill;
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
    required this.orderNumber,
    required this.currency,
    required this.sessionId,
    required this.orderCount,
    required this.waiterName,
  });

  factory TableOverviewRow.fromJson(Map<String, dynamic> j) => TableOverviewRow(
        tableId: _s(j['tableId']),
        tableNumber: _s(j['tableNumber']),
        capacity: _i(j['capacity']),
        status: _s(j['status']),
        guestCount: _iOrNull(j['guestCount']),
        sessionMinutes: _iOrNull(j['sessionMinutes']),
        currentBill: _d(j['currentBill']),
        orderNumber: _sOrNull(j['orderNumber']),
        currency: j['currency'] as String? ?? 'Tk',
        sessionId: _sOrNull(j['sessionId']),
        orderCount: _i(j['orderCount']),
        waiterName: _sOrNull(j['waiterName']),
      );
}

// ---------- section 5: kitchen ----------
class KitchenPerformance {
  final double averagePrepMinutes;
  final int ordersWaitingOver10Min;
  final int? longestWaitingMinutes;
  final String? longestWaitingOrderNumber;
  final int completedToday;
  final int pendingCount;
  final int preparingCount;
  final int readyCount;

  KitchenPerformance({
    required this.averagePrepMinutes,
    required this.ordersWaitingOver10Min,
    required this.longestWaitingMinutes,
    required this.longestWaitingOrderNumber,
    required this.completedToday,
    required this.pendingCount,
    required this.preparingCount,
    required this.readyCount,
  });

  factory KitchenPerformance.fromJson(Map<String, dynamic> j) => KitchenPerformance(
        averagePrepMinutes: _d(j['averagePrepMinutes']),
        ordersWaitingOver10Min: _i(j['ordersWaitingOver10Min']),
        longestWaitingMinutes: _iOrNull(j['longestWaitingMinutes']),
        longestWaitingOrderNumber: _sOrNull(j['longestWaitingOrderNumber']),
        completedToday: _i(j['completedToday']),
        pendingCount: _i(j['pendingCount']),
        preparingCount: _i(j['preparingCount']),
        readyCount: _i(j['readyCount']),
      );
}

// ---------- section 4: live orders ----------
class LiveOrderRow {
  final String id;
  final String orderNumber;
  final String? tableNumber;
  final String orderType;
  final String channel; // QR | Staff
  final DateTime orderedAtUtc;
  final double total;
  final String status;
  final bool isPaid;
  final String currency;

  LiveOrderRow({
    required this.id,
    required this.orderNumber,
    required this.tableNumber,
    required this.orderType,
    required this.channel,
    required this.orderedAtUtc,
    required this.total,
    required this.status,
    required this.isPaid,
    required this.currency,
  });

  factory LiveOrderRow.fromJson(Map<String, dynamic> j) => LiveOrderRow(
        id: _s(j['id']),
        orderNumber: _s(j['orderNumber']),
        tableNumber: _sOrNull(j['tableNumber']),
        orderType: _s(j['orderType']),
        channel: _s(j['channel']),
        orderedAtUtc: DateTime.tryParse(_s(j['orderedAtUtc']))?.toLocal() ?? DateTime.now(),
        total: _d(j['total']),
        status: _s(j['status']),
        isPaid: j['isPaid'] == true,
        currency: j['currency'] as String? ?? 'Tk',
      );
}

// ---------- section 6: customer requests ----------
class CustomerRequestRow {
  final String id;
  final String tableNumber;
  final String type;
  final String status;
  final int waitingMinutes;
  final String? note;

  CustomerRequestRow({
    required this.id,
    required this.tableNumber,
    required this.type,
    required this.status,
    required this.waitingMinutes,
    required this.note,
  });

  factory CustomerRequestRow.fromJson(Map<String, dynamic> j) => CustomerRequestRow(
        id: _s(j['id']),
        tableNumber: _s(j['tableNumber']),
        type: _s(j['type']),
        status: _s(j['status']),
        waitingMinutes: _i(j['waitingMinutes']),
        note: _sOrNull(j['note']),
      );
}

// ---------- section 7: inventory alerts ----------
class StockAlertRow {
  final String code;
  final String name;
  final String unitCode;
  final double qtyOnHand;
  final double reorderLevel;
  StockAlertRow({required this.code, required this.name, required this.unitCode, required this.qtyOnHand, required this.reorderLevel});

  factory StockAlertRow.fromJson(Map<String, dynamic> j) => StockAlertRow(
        code: _s(j['code']),
        name: _s(j['name']),
        unitCode: _s(j['unitCode']),
        qtyOnHand: _d(j['qtyOnHand']),
        reorderLevel: _d(j['reorderLevel']),
      );
}

class ConsumptionRow {
  final String code;
  final String name;
  final String unitCode;
  final double qtyConsumed;
  ConsumptionRow({required this.code, required this.name, required this.unitCode, required this.qtyConsumed});

  factory ConsumptionRow.fromJson(Map<String, dynamic> j) => ConsumptionRow(
        code: _s(j['code']),
        name: _s(j['name']),
        unitCode: _s(j['unitCode']),
        qtyConsumed: _d(j['qtyConsumed']),
      );
}

class InventoryAlerts {
  final List<StockAlertRow> lowStock;
  final List<StockAlertRow> outOfStock;
  final List<ConsumptionRow> todaysConsumption;
  InventoryAlerts({required this.lowStock, required this.outOfStock, required this.todaysConsumption});

  factory InventoryAlerts.fromJson(Map<String, dynamic> j) => InventoryAlerts(
        lowStock: (j['lowStock'] as List? ?? []).map((e) => StockAlertRow.fromJson(e as Map<String, dynamic>)).toList(),
        outOfStock: (j['outOfStock'] as List? ?? []).map((e) => StockAlertRow.fromJson(e as Map<String, dynamic>)).toList(),
        todaysConsumption: (j['todaysConsumption'] as List? ?? []).map((e) => ConsumptionRow.fromJson(e as Map<String, dynamic>)).toList(),
      );
}

// ---------- section 8: staff leaderboard ----------
class StaffActivityRow {
  final String waiterName;
  final int ordersProcessed;
  final int tablesAssigned;
  final double revenue;
  StaffActivityRow({required this.waiterName, required this.ordersProcessed, required this.tablesAssigned, required this.revenue});

  factory StaffActivityRow.fromJson(Map<String, dynamic> j) => StaffActivityRow(
        waiterName: _s(j['waiterName']),
        ordersProcessed: _i(j['ordersProcessed']),
        tablesAssigned: _i(j['tablesAssigned']),
        revenue: _d(j['revenue']),
      );
}

// ---------- section 3: analytics ----------
class HourlySales {
  final int hour;
  final double revenue;
  final int orderCount;
  HourlySales({required this.hour, required this.revenue, required this.orderCount});

  factory HourlySales.fromJson(Map<String, dynamic> j) => HourlySales(
        hour: _i(j['hour']),
        revenue: _d(j['revenue']),
        orderCount: _i(j['orderCount']),
      );
}

class CategorySales {
  final String category;
  final double revenue;
  final int quantity;
  CategorySales({required this.category, required this.revenue, required this.quantity});

  factory CategorySales.fromJson(Map<String, dynamic> j) => CategorySales(
        category: _s(j['category']),
        revenue: _d(j['revenue']),
        quantity: _i(j['quantity']),
      );
}

class TopItemRow {
  final String code;
  final String name;
  final int quantitySold;
  final double revenue;
  final String currency;
  TopItemRow({required this.code, required this.name, required this.quantitySold, required this.revenue, required this.currency});

  factory TopItemRow.fromJson(Map<String, dynamic> j) => TopItemRow(
        code: _s(j['code']),
        name: _s(j['name']),
        quantitySold: _i(j['quantitySold']),
        revenue: _d(j['revenue']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

class RevenueBreakdown {
  final double dineInRevenue;
  final double takeawayRevenue;
  final double deliveryRevenue;
  final double qrOrderingRevenue;
  final double discountAmount;
  final double taxCollected;
  final double serviceChargeCollected;
  final double grandTotal;
  final String currency;

  RevenueBreakdown({
    required this.dineInRevenue,
    required this.takeawayRevenue,
    required this.deliveryRevenue,
    required this.qrOrderingRevenue,
    required this.discountAmount,
    required this.taxCollected,
    required this.serviceChargeCollected,
    required this.grandTotal,
    required this.currency,
  });

  factory RevenueBreakdown.fromJson(Map<String, dynamic> j) => RevenueBreakdown(
        dineInRevenue: _d(j['dineInRevenue']),
        takeawayRevenue: _d(j['takeawayRevenue']),
        deliveryRevenue: _d(j['deliveryRevenue']),
        qrOrderingRevenue: _d(j['qrOrderingRevenue']),
        discountAmount: _d(j['discountAmount']),
        taxCollected: _d(j['taxCollected']),
        serviceChargeCollected: _d(j['serviceChargeCollected']),
        grandTotal: _d(j['grandTotal']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

// ---------- orders module ----------
class OrderListItem {
  final String id;
  final String orderNumber;
  final String customerPhone;
  final String? customerName;
  final String? tableNumber;
  final String orderType;
  final String status;
  final DateTime orderedAtUtc;
  final String currency;
  final int itemCount;
  final double total;
  final bool isPaid;

  OrderListItem({
    required this.id,
    required this.orderNumber,
    required this.customerPhone,
    required this.customerName,
    required this.tableNumber,
    required this.orderType,
    required this.status,
    required this.orderedAtUtc,
    required this.currency,
    required this.itemCount,
    required this.total,
    required this.isPaid,
  });

  factory OrderListItem.fromJson(Map<String, dynamic> j) => OrderListItem(
        id: _s(j['id']),
        orderNumber: _s(j['orderNumber']),
        customerPhone: _s(j['customerPhone']),
        customerName: _sOrNull(j['customerName']),
        tableNumber: _sOrNull(j['tableNumber']),
        orderType: _s(j['orderType']),
        status: _s(j['status']),
        orderedAtUtc: DateTime.tryParse(_s(j['orderedAtUtc']))?.toLocal() ?? DateTime.now(),
        currency: j['currency'] as String? ?? 'Tk',
        itemCount: _i(j['itemCount']),
        total: _d(j['total']),
        isPaid: j['isPaid'] == true,
      );
}

class OrderLineModifier {
  final String groupName;
  final String optionName;
  final double priceDelta;
  final String? optionId;
  OrderLineModifier({required this.groupName, required this.optionName, required this.priceDelta, required this.optionId});

  factory OrderLineModifier.fromJson(Map<String, dynamic> j) => OrderLineModifier(
        groupName: _s(j['groupName']),
        optionName: _s(j['optionName']),
        priceDelta: _d(j['priceDelta']),
        optionId: _sOrNull(j['optionId']),
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
  final String? imagePath;
  final List<OrderLineModifier> modifiers;
  OrderLine({
    required this.menuItemId,
    required this.variantId,
    required this.code,
    required this.name,
    required this.unitPrice,
    required this.quantity,
    required this.lineTotal,
    required this.notes,
    required this.imagePath,
    required this.modifiers,
  });

  factory OrderLine.fromJson(Map<String, dynamic> j) => OrderLine(
        menuItemId: _s(j['menuItemId']),
        variantId: _sOrNull(j['variantId']),
        code: _s(j['code']),
        name: _s(j['name']),
        unitPrice: _d(j['unitPrice']),
        quantity: _i(j['quantity']),
        lineTotal: _d(j['lineTotal']),
        notes: _sOrNull(j['notes']),
        imagePath: _sOrNull(j['imagePath']),
        modifiers: (j['modifiers'] as List? ?? [])
            .map((e) => OrderLineModifier.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class OrderDetail {
  final String id;
  final String orderNumber;
  final String customerPhone;
  final String? customerName;
  final String? tableNumber;
  final String orderType;
  final String status;
  final DateTime orderedAtUtc;
  final String currency;
  final String? notes;
  final double subtotal;
  final double discountAmount;
  final double taxAmount;
  final double serviceChargeAmount;
  final double tipAmount;
  final double roundingAdjustment;
  final double grandTotal;
  final bool isPaid;
  final double amountPaid;
  final double balanceDue;
  final String? paymentMethod;
  final String? waiterName;
  final List<OrderLine> lines;

  OrderDetail({
    required this.id,
    required this.orderNumber,
    required this.customerPhone,
    required this.customerName,
    required this.tableNumber,
    required this.orderType,
    required this.status,
    required this.orderedAtUtc,
    required this.currency,
    required this.notes,
    required this.subtotal,
    required this.discountAmount,
    required this.taxAmount,
    required this.serviceChargeAmount,
    required this.tipAmount,
    required this.roundingAdjustment,
    required this.grandTotal,
    required this.isPaid,
    required this.amountPaid,
    required this.balanceDue,
    required this.paymentMethod,
    required this.waiterName,
    required this.lines,
  });

  factory OrderDetail.fromJson(Map<String, dynamic> j) => OrderDetail(
        id: _s(j['id']),
        orderNumber: _s(j['orderNumber']),
        customerPhone: _s(j['customerPhone']),
        customerName: _sOrNull(j['customerName']),
        tableNumber: _sOrNull(j['tableNumber']),
        orderType: _s(j['orderType']),
        status: _s(j['status']),
        orderedAtUtc: DateTime.tryParse(_s(j['orderedAtUtc']))?.toLocal() ?? DateTime.now(),
        currency: j['currency'] as String? ?? 'Tk',
        notes: _sOrNull(j['notes']),
        subtotal: _d(j['subtotal']),
        discountAmount: _d(j['discountAmount']),
        taxAmount: _d(j['taxAmount']),
        serviceChargeAmount: _d(j['serviceChargeAmount']),
        tipAmount: _d(j['tipAmount']),
        roundingAdjustment: _d(j['roundingAdjustment']),
        grandTotal: _d(j['grandTotal']),
        isPaid: j['isPaid'] == true,
        amountPaid: _d(j['amountPaid']),
        balanceDue: _d(j['balanceDue']),
        paymentMethod: _sOrNull(j['paymentMethod']),
        waiterName: _sOrNull(j['waiterName']),
        lines: (j['lines'] as List? ?? []).map((e) => OrderLine.fromJson(e as Map<String, dynamic>)).toList(),
      );
}

// ---------- navigation menu (DB-driven) ----------
class MenuItem {
  final String id;
  final String title;
  final String? url;
  final String? icon; // Fluent UI icon name
  final int displayOrder;
  final String? requiredRole;
  final List<MenuItem> children;

  MenuItem({
    required this.id,
    required this.title,
    required this.url,
    required this.icon,
    required this.displayOrder,
    required this.requiredRole,
    required this.children,
  });

  bool get hasChildren => children.isNotEmpty;

  factory MenuItem.fromJson(Map<String, dynamic> j) => MenuItem(
        id: _s(j['id']),
        title: _s(j['title']),
        url: _sOrNull(j['url']),
        icon: _sOrNull(j['icon']),
        displayOrder: _i(j['displayOrder']),
        requiredRole: _sOrNull(j['requiredRole']),
        children: (j['children'] as List? ?? [])
            .map((e) => MenuItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// A single fetch of every dashboard section (one poll).
class DashboardData {
  final DashboardSummary summary;
  final List<TableOverviewRow> tables;
  final KitchenPerformance kitchen;
  final Paged<LiveOrderRow> orders;
  final List<CustomerRequestRow> requests;
  final InventoryAlerts inventory;
  final List<StaffActivityRow> staff;
  final List<HourlySales> salesByHour;
  final List<CategorySales> salesByCategory;
  final List<TopItemRow> topItems;
  final RevenueBreakdown revenue;

  DashboardData({
    required this.summary,
    required this.tables,
    required this.kitchen,
    required this.orders,
    required this.requests,
    required this.inventory,
    required this.staff,
    required this.salesByHour,
    required this.salesByCategory,
    required this.topItems,
    required this.revenue,
  });
}
