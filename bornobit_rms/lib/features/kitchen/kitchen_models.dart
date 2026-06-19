// DTOs for the Kitchen Display (KDS) — mirrors the Blazor KitchenDisplay.razor.
// JSON field names match the C# record property names (camelCase): see
// Application/Kitchen/Queries (KitchenBoardDto, KitchenOrderCardDto, KitchenItemDto,
// KitchenStationDto) and Operations/Dashboard/KitchenPerformanceDto.

double _d(dynamic v) => v == null ? 0 : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
int? _iOrNull(dynamic v) => v == null ? null : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();
DateTime? _dt(dynamic v) => v == null ? null : DateTime.tryParse(v.toString())?.toLocal();

/// A kitchen station (board tab + per-line routing).
class KitchenStation {
  final String id;
  final String name;
  final String? code;
  final String? colorHex;
  final int displayOrder;
  final bool isActive;

  KitchenStation({
    required this.id,
    required this.name,
    required this.code,
    required this.colorHex,
    required this.displayOrder,
    required this.isActive,
  });

  factory KitchenStation.fromJson(Map<String, dynamic> j) => KitchenStation(
        id: _s(j['id']),
        name: _s(j['name']),
        code: _sOrNull(j['code']),
        colorHex: _sOrNull(j['colorHex']),
        displayOrder: _i(j['displayOrder']),
        isActive: j['isActive'] as bool? ?? true,
      );
}

/// One line item on a kitchen ticket.
class KitchenItem {
  final int quantity;
  final String name;
  final String? notes;
  final String? stationId;
  final String? stationName;

  KitchenItem({
    required this.quantity,
    required this.name,
    required this.notes,
    required this.stationId,
    required this.stationName,
  });

  factory KitchenItem.fromJson(Map<String, dynamic> j) => KitchenItem(
        quantity: _i(j['quantity']),
        name: _s(j['name']),
        notes: _sOrNull(j['notes']),
        stationId: _sOrNull(j['stationId']),
        stationName: _sOrNull(j['stationName']),
      );
}

/// An order card on the kitchen board.
class KitchenOrderCard {
  final String id;
  final String orderNumber;
  final String orderType; // DineIn | Takeaway | Delivery | Collection | Waiting
  final String status; // Placed | Confirmed | Preparing | Ready
  final String? tableNumber;
  final String? customerName;
  final DateTime? orderedAtUtc;
  final DateTime? preparingAtUtc;
  final DateTime? readyAtUtc;
  final bool isPriority;
  final String? kitchenNotes;
  final String? customerNotes;
  final String source;
  final int itemCount;
  final List<KitchenItem> items;

  KitchenOrderCard({
    required this.id,
    required this.orderNumber,
    required this.orderType,
    required this.status,
    required this.tableNumber,
    required this.customerName,
    required this.orderedAtUtc,
    required this.preparingAtUtc,
    required this.readyAtUtc,
    required this.isPriority,
    required this.kitchenNotes,
    required this.customerNotes,
    required this.source,
    required this.itemCount,
    required this.items,
  });

  factory KitchenOrderCard.fromJson(Map<String, dynamic> j) => KitchenOrderCard(
        id: _s(j['id']),
        orderNumber: _s(j['orderNumber']),
        orderType: _s(j['orderType']),
        status: _s(j['status']),
        tableNumber: _sOrNull(j['tableNumber']),
        customerName: _sOrNull(j['customerName']),
        orderedAtUtc: _dt(j['orderedAtUtc']),
        preparingAtUtc: _dt(j['preparingAtUtc']),
        readyAtUtc: _dt(j['readyAtUtc']),
        isPriority: j['isPriority'] as bool? ?? false,
        kitchenNotes: _sOrNull(j['kitchenNotes']),
        customerNotes: _sOrNull(j['customerNotes']),
        source: _s(j['source']),
        itemCount: _i(j['itemCount']),
        items: (j['items'] as List? ?? [])
            .map((e) => KitchenItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  /// Minutes elapsed since the order was placed, relative to [now] (UTC).
  int elapsedMinutes(DateTime nowUtc) {
    final t = orderedAtUtc;
    if (t == null) return 0;
    return nowUtc.difference(t.toUtc()).inMinutes;
  }
}

/// The live board: orders grouped into Pending / Preparing / Ready columns.
class KitchenBoard {
  final List<KitchenOrderCard> pending;
  final List<KitchenOrderCard> preparing;
  final List<KitchenOrderCard> ready;

  KitchenBoard({
    required this.pending,
    required this.preparing,
    required this.ready,
  });

  factory KitchenBoard.fromJson(Map<String, dynamic> j) => KitchenBoard(
        pending: _cards(j['pending']),
        preparing: _cards(j['preparing']),
        ready: _cards(j['ready']),
      );

  static List<KitchenOrderCard> _cards(dynamic v) => (v as List? ?? [])
      .map((e) => KitchenOrderCard.fromJson(e as Map<String, dynamic>))
      .toList();
}

/// Kitchen KPIs (GetKitchenPerformanceQuery → KitchenPerformanceDto).
class KitchenMetrics {
  final double averagePrepMinutes;
  final int ordersWaitingOver10Min;
  final int? longestWaitingMinutes;
  final String? longestWaitingOrderNumber;
  final int completedToday;
  final int pendingCount;
  final int preparingCount;
  final int readyCount;

  KitchenMetrics({
    required this.averagePrepMinutes,
    required this.ordersWaitingOver10Min,
    required this.longestWaitingMinutes,
    required this.longestWaitingOrderNumber,
    required this.completedToday,
    required this.pendingCount,
    required this.preparingCount,
    required this.readyCount,
  });

  factory KitchenMetrics.fromJson(Map<String, dynamic> j) => KitchenMetrics(
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

/// Console aggregate (`GET /staff/kitchen/console`): board + stations + metrics in one round-trip.
class KitchenConsole {
  final KitchenBoard board;
  final List<KitchenStation> stations;
  final KitchenMetrics metrics;

  KitchenConsole({
    required this.board,
    required this.stations,
    required this.metrics,
  });

  factory KitchenConsole.fromJson(Map<String, dynamic> j) => KitchenConsole(
        board: KitchenBoard.fromJson(j['board'] as Map<String, dynamic>),
        stations: (j['stations'] as List? ?? [])
            .map((e) => KitchenStation.fromJson(e as Map<String, dynamic>))
            .toList(),
        metrics: KitchenMetrics.fromJson(j['metrics'] as Map<String, dynamic>),
      );
}
