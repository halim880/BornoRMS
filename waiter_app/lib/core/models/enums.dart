// Domain enums. The API serializes enums as STRINGS (JsonStringEnumConverter),
// using the exact C# member name (e.g. "DineIn", "WaitingPayment"). Each enum
// keeps the wire name and parses unknown values to a safe sentinel so a future
// backend value never crashes the app.

enum OrderType {
  dineIn('DineIn', 'Dine-in'),
  takeaway('Takeaway', 'Takeaway'),
  delivery('Delivery', 'Delivery'),
  collection('Collection', 'Collection'),
  waiting('Waiting', 'Waiting'),
  unknown('Unknown', 'Unknown');

  const OrderType(this.wire, this.label);
  final String wire;
  final String label;

  static OrderType fromName(String? s) =>
      OrderType.values.firstWhere((e) => e.wire == s, orElse: () => OrderType.unknown);
}

enum OrderStatus {
  placed('Placed', 'Placed'),
  confirmed('Confirmed', 'Confirmed'),
  preparing('Preparing', 'Preparing'),
  ready('Ready', 'Ready'),
  served('Served', 'Served'),
  completed('Completed', 'Completed'),
  cancelled('Cancelled', 'Cancelled'),
  unknown('Unknown', 'Unknown');

  const OrderStatus(this.wire, this.label);
  final String wire;
  final String label;

  static OrderStatus fromName(String? s) =>
      OrderStatus.values.firstWhere((e) => e.wire == s, orElse: () => OrderStatus.unknown);
}

enum DerivedTableStatus {
  available('Available', 'Available'),
  occupied('Occupied', 'Occupied'),
  reserved('Reserved', 'Reserved'),
  waitingPayment('WaitingPayment', 'Waiting payment'),
  unknown('Unknown', 'Unknown');

  const DerivedTableStatus(this.wire, this.label);
  final String wire;
  final String label;

  static DerivedTableStatus fromName(String? s) =>
      DerivedTableStatus.values.firstWhere((e) => e.wire == s, orElse: () => DerivedTableStatus.unknown);
}

enum CustomerRequestType {
  callWaiter('CallWaiter', 'Call waiter'),
  requestBill('RequestBill', 'Request bill'),
  needWater('NeedWater', 'Need water'),
  needTissue('NeedTissue', 'Need tissue'),
  unknown('Unknown', 'Request');

  const CustomerRequestType(this.wire, this.label);
  final String wire;
  final String label;

  static CustomerRequestType fromName(String? s) =>
      CustomerRequestType.values.firstWhere((e) => e.wire == s, orElse: () => CustomerRequestType.unknown);
}

enum DiningSessionStatus {
  open('Open', 'Open'),
  billing('Billing', 'Billing'),
  closed('Closed', 'Closed'),
  merged('Merged', 'Merged'),
  unknown('Unknown', 'Unknown');

  const DiningSessionStatus(this.wire, this.label);
  final String wire;
  final String label;

  static DiningSessionStatus fromName(String? s) =>
      DiningSessionStatus.values.firstWhere((e) => e.wire == s, orElse: () => DiningSessionStatus.unknown);
}
