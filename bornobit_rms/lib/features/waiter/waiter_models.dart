// DTOs for the waiter console (mirrors the Blazor WaiterOrders.razor). Floor rows
// and customer requests reuse the shared dashboard models (TableOverviewRow,
// CustomerRequestRow) in core/models/dtos.dart.

double _d(dynamic v) => v == null ? 0 : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();
DateTime? _dt(dynamic v) => v == null ? null : DateTime.tryParse(v.toString())?.toLocal();

/// Top-strip counters for the waiter console.
class WaiterDashboard {
  final int myTables;
  final int availableTables;
  final int occupiedTables;
  final int pendingRequests;
  final int readyToServeOrders;
  final int billsWaiting;
  final int myActiveSessions;
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

/// A dining session on the floor / "my sessions" list.
class SessionRow {
  final String id;
  final String sessionNumber;
  final String restaurantTableId;
  final String tableNumber;
  final int guestCount;
  final String? waiterUserId;
  final String? waiterName;
  final String status; // Open | Billing | Closed
  final DateTime? openedAtUtc;
  final int sessionMinutes;
  final int orderCount;
  final double runningBill;
  final String currency;

  SessionRow({
    required this.id,
    required this.sessionNumber,
    required this.restaurantTableId,
    required this.tableNumber,
    required this.guestCount,
    required this.waiterUserId,
    required this.waiterName,
    required this.status,
    required this.openedAtUtc,
    required this.sessionMinutes,
    required this.orderCount,
    required this.runningBill,
    required this.currency,
  });

  factory SessionRow.fromJson(Map<String, dynamic> j) => SessionRow(
        id: _s(j['id']),
        sessionNumber: _s(j['sessionNumber']),
        restaurantTableId: _s(j['restaurantTableId']),
        tableNumber: _s(j['tableNumber']),
        guestCount: _i(j['guestCount']),
        waiterUserId: _sOrNull(j['waiterUserId']),
        waiterName: _sOrNull(j['waiterName']),
        status: _s(j['status']),
        openedAtUtc: _dt(j['openedAtUtc']),
        sessionMinutes: _i(j['sessionMinutes']),
        orderCount: _i(j['orderCount']),
        runningBill: _d(j['runningBill']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

/// An order cooked and ready to carry to the table.
class ReadyToServeRow {
  final String orderId;
  final String orderNumber;
  final String? tableNumber;
  final String? diningSessionId;
  final DateTime? readyAtUtc;
  final int waitingMinutes;
  final List<ReadyToServeLine> items;

  ReadyToServeRow({
    required this.orderId,
    required this.orderNumber,
    required this.tableNumber,
    required this.diningSessionId,
    required this.readyAtUtc,
    required this.waitingMinutes,
    required this.items,
  });

  factory ReadyToServeRow.fromJson(Map<String, dynamic> j) => ReadyToServeRow(
        orderId: _s(j['orderId']),
        orderNumber: _s(j['orderNumber']),
        tableNumber: _sOrNull(j['tableNumber']),
        diningSessionId: _sOrNull(j['diningSessionId']),
        readyAtUtc: _dt(j['readyAtUtc']),
        waitingMinutes: _i(j['waitingMinutes']),
        items: (j['items'] as List? ?? [])
            .map((e) => ReadyToServeLine.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class ReadyToServeLine {
  final String name;
  final int quantity;
  final String? stationName;
  ReadyToServeLine({required this.name, required this.quantity, required this.stationName});

  factory ReadyToServeLine.fromJson(Map<String, dynamic> j) => ReadyToServeLine(
        name: _s(j['name']),
        quantity: _i(j['quantity']),
        stationName: _sOrNull(j['stationName']),
      );
}

/// Running bill for a session.
class SessionBill {
  final String sessionId;
  final String sessionNumber;
  final String tableNumber;
  final int guestCount;
  final List<SessionBillOrder> orders;
  final double subtotal;
  final double discountAmount;
  final double taxAmount;
  final double serviceChargeAmount;
  final double roundingAdjustment;
  final double grandTotal;
  final double paidAmount;
  final double balanceDue;
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
    required this.roundingAdjustment,
    required this.grandTotal,
    required this.paidAmount,
    required this.balanceDue,
    required this.currency,
  });

  factory SessionBill.fromJson(Map<String, dynamic> j) => SessionBill(
        sessionId: _s(j['sessionId']),
        sessionNumber: _s(j['sessionNumber']),
        tableNumber: _s(j['tableNumber']),
        guestCount: _i(j['guestCount']),
        orders: (j['orders'] as List? ?? [])
            .map((e) => SessionBillOrder.fromJson(e as Map<String, dynamic>))
            .toList(),
        subtotal: _d(j['subtotal']),
        discountAmount: _d(j['discountAmount']),
        taxAmount: _d(j['taxAmount']),
        serviceChargeAmount: _d(j['serviceChargeAmount']),
        roundingAdjustment: _d(j['roundingAdjustment']),
        grandTotal: _d(j['grandTotal']),
        paidAmount: _d(j['paidAmount']),
        balanceDue: _d(j['balanceDue']),
        currency: j['currency'] as String? ?? 'Tk',
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
        orderId: _s(j['orderId']),
        orderNumber: _s(j['orderNumber']),
        status: _s(j['status']),
        isPaid: j['isPaid'] as bool? ?? false,
        orderTotal: _d(j['orderTotal']),
        lines: (j['lines'] as List? ?? [])
            .map((e) => SessionBillLine.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class SessionBillLine {
  final String name;
  final int quantity;
  final double unitPrice;
  final double lineTotal;
  SessionBillLine({
    required this.name,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
  });

  factory SessionBillLine.fromJson(Map<String, dynamic> j) => SessionBillLine(
        name: _s(j['name']),
        quantity: _i(j['quantity']),
        unitPrice: _d(j['unitPrice']),
        lineTotal: _d(j['lineTotal']),
      );
}

/// Console aggregate (`GET /waiter/console`): four polled reads in one round-trip.
class WaiterConsole {
  final WaiterDashboard dashboard;
  final List<dynamic> floorRaw; // parsed by screen via TableOverviewRow
  final List<ReadyToServeRow> ready;
  final List<dynamic> requestsRaw; // parsed by screen via CustomerRequestRow
  WaiterConsole({
    required this.dashboard,
    required this.floorRaw,
    required this.ready,
    required this.requestsRaw,
  });
}
