// DTOs for the Delivery module. Field names mirror the C# records in
// BornoBit.Restaurant.Application.Logistics (serialized camelCase). Enums are
// serialized as strings (JsonStringEnumConverter).

double _d(dynamic v) => v == null ? 0.0 : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();
DateTime _dt(dynamic v) => v == null ? DateTime.now() : DateTime.parse(v as String).toLocal();
DateTime? _dtOrNull(dynamic v) => v == null ? null : DateTime.parse(v as String).toLocal();

/// Dispatch lifecycle — mirrors Domain.Logistics.DeliveryStatus.
enum DeliveryStatus { pending, assigned, outForDelivery, delivered, failed, cancelled, unknown }

DeliveryStatus deliveryStatusFromJson(dynamic v) {
  switch (_s(v)) {
    case 'Pending':
      return DeliveryStatus.pending;
    case 'Assigned':
      return DeliveryStatus.assigned;
    case 'OutForDelivery':
      return DeliveryStatus.outForDelivery;
    case 'Delivered':
      return DeliveryStatus.delivered;
    case 'Failed':
      return DeliveryStatus.failed;
    case 'Cancelled':
      return DeliveryStatus.cancelled;
    default:
      return DeliveryStatus.unknown;
  }
}

extension DeliveryStatusLabel on DeliveryStatus {
  String get label => switch (this) {
        DeliveryStatus.pending => 'Pending',
        DeliveryStatus.assigned => 'Assigned',
        DeliveryStatus.outForDelivery => 'Out for delivery',
        DeliveryStatus.delivered => 'Delivered',
        DeliveryStatus.failed => 'Failed',
        DeliveryStatus.cancelled => 'Cancelled',
        DeliveryStatus.unknown => '—',
      };
}

/// One dispatch-board row (DeliveryBoardRow).
class DeliveryBoardRow {
  final String orderId;
  final String orderNumber;
  final DeliveryStatus deliveryStatus;
  final String orderStatus;
  final String? riderId;
  final String? riderName;
  final String address;
  final String? contactPhone;
  final double grandTotal;
  final double amountPaid;
  final double codExpected;
  final bool isPaid;
  final DateTime orderedAtUtc;
  final DateTime? outForDeliveryAtUtc;

  DeliveryBoardRow({
    required this.orderId,
    required this.orderNumber,
    required this.deliveryStatus,
    required this.orderStatus,
    required this.riderId,
    required this.riderName,
    required this.address,
    required this.contactPhone,
    required this.grandTotal,
    required this.amountPaid,
    required this.codExpected,
    required this.isPaid,
    required this.orderedAtUtc,
    required this.outForDeliveryAtUtc,
  });

  factory DeliveryBoardRow.fromJson(Map<String, dynamic> j) => DeliveryBoardRow(
        orderId: _s(j['orderId']),
        orderNumber: _s(j['orderNumber']),
        deliveryStatus: deliveryStatusFromJson(j['deliveryStatus']),
        orderStatus: _s(j['orderStatus']),
        riderId: _sOrNull(j['riderId']),
        riderName: _sOrNull(j['riderName']),
        address: _s(j['address']),
        contactPhone: _sOrNull(j['contactPhone']),
        grandTotal: _d(j['grandTotal']),
        amountPaid: _d(j['amountPaid']),
        codExpected: _d(j['codExpected']),
        isPaid: j['isPaid'] == true,
        orderedAtUtc: _dt(j['orderedAtUtc']),
        outForDeliveryAtUtc: _dtOrNull(j['outForDeliveryAtUtc']),
      );

  /// COD is expected when an unpaid delivery still owes money.
  bool get isCod => !isPaid && codExpected > 0;
}

/// A rider on the roster (RiderDto).
class Rider {
  final String id;
  final String name;
  final String phone;
  final String? vehicle;
  final bool isActive;

  Rider({required this.id, required this.name, required this.phone, required this.vehicle, required this.isActive});

  factory Rider.fromJson(Map<String, dynamic> j) => Rider(
        id: _s(j['id']),
        name: _s(j['name']),
        phone: _s(j['phone']),
        vehicle: _sOrNull(j['vehicle']),
        isActive: j['isActive'] == true,
      );
}

/// One rider's COD reconciliation row (RiderCodRow).
class RiderCodRow {
  final String riderId;
  final String riderName;
  final int outstandingCount;
  final double outstandingCod;
  final int collectedCount;
  final double collectedToday;

  RiderCodRow({
    required this.riderId,
    required this.riderName,
    required this.outstandingCount,
    required this.outstandingCod,
    required this.collectedCount,
    required this.collectedToday,
  });

  factory RiderCodRow.fromJson(Map<String, dynamic> j) => RiderCodRow(
        riderId: _s(j['riderId']),
        riderName: _s(j['riderName']),
        outstandingCount: _i(j['outstandingCount']),
        outstandingCod: _d(j['outstandingCod']),
        collectedCount: _i(j['collectedCount']),
        collectedToday: _d(j['collectedToday']),
      );
}
