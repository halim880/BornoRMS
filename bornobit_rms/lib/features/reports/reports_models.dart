// DTOs for the Operations → Reports screens. Field names mirror the C# records
// in BornoBit.Restaurant.Application.Ordering.Queries (serialized camelCase).

// ---- shared parse helpers (copied from the feature convention) ----
double _d(dynamic v) => v == null ? 0.0 : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();
DateTime _dt(dynamic v) => v == null ? DateTime.now() : DateTime.parse(v as String).toLocal();

/// One day of paid sales (SalesReportRowDto).
class SalesReportRow {
  final DateTime date;
  final int orderCount;
  final double subtotal;
  final double discount;
  final double total;

  SalesReportRow({
    required this.date,
    required this.orderCount,
    required this.subtotal,
    required this.discount,
    required this.total,
  });

  factory SalesReportRow.fromJson(Map<String, dynamic> j) => SalesReportRow(
        // DateOnly serializes as "yyyy-MM-dd" — parse as a plain date, no TZ shift.
        date: DateTime.parse(_s(j['date'])),
        orderCount: _i(j['orderCount']),
        subtotal: _d(j['subtotal']),
        discount: _d(j['discount']),
        total: _d(j['total']),
      );
}

/// Sales report (SalesReportDto).
class SalesReport {
  final List<SalesReportRow> rows;
  final int totalOrders;
  final double totalSubtotal;
  final double totalDiscount;
  final double grandTotal;
  final String currency;

  SalesReport({
    required this.rows,
    required this.totalOrders,
    required this.totalSubtotal,
    required this.totalDiscount,
    required this.grandTotal,
    required this.currency,
  });

  factory SalesReport.fromJson(Map<String, dynamic> j) => SalesReport(
        rows: ((j['rows'] as List?) ?? [])
            .map((e) => SalesReportRow.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalOrders: _i(j['totalOrders']),
        totalSubtotal: _d(j['totalSubtotal']),
        totalDiscount: _d(j['totalDiscount']),
        grandTotal: _d(j['grandTotal']),
        currency: _s(j['currency']),
      );
}

/// One invoice line (SalesInvoiceRowDto).
class SalesInvoiceRow {
  final String orderId;
  final String orderNumber;
  final DateTime paidAtUtc;
  final String? customerName;
  final String customerPhone;
  final String orderType;
  final String? paymentMethod;
  final double subtotal;
  final double discount;
  final double total;

  SalesInvoiceRow({
    required this.orderId,
    required this.orderNumber,
    required this.paidAtUtc,
    required this.customerName,
    required this.customerPhone,
    required this.orderType,
    required this.paymentMethod,
    required this.subtotal,
    required this.discount,
    required this.total,
  });

  factory SalesInvoiceRow.fromJson(Map<String, dynamic> j) => SalesInvoiceRow(
        orderId: _s(j['orderId']),
        orderNumber: _s(j['orderNumber']),
        paidAtUtc: _dt(j['paidAtUtc']),
        customerName: _sOrNull(j['customerName']),
        customerPhone: _s(j['customerPhone']),
        orderType: _s(j['orderType']),
        paymentMethod: _sOrNull(j['paymentMethod']),
        subtotal: _d(j['subtotal']),
        discount: _d(j['discount']),
        total: _d(j['total']),
      );

  /// Display name preferring the customer name, falling back to the phone.
  String get customerLabel =>
      (customerName == null || customerName!.trim().isEmpty) ? customerPhone : customerName!;
}

/// Invoice-wise sales report (SalesInvoiceReportDto).
class SalesInvoiceReport {
  final List<SalesInvoiceRow> rows;
  final int totalInvoices;
  final double totalSubtotal;
  final double totalDiscount;
  final double grandTotal;
  final String currency;

  SalesInvoiceReport({
    required this.rows,
    required this.totalInvoices,
    required this.totalSubtotal,
    required this.totalDiscount,
    required this.grandTotal,
    required this.currency,
  });

  factory SalesInvoiceReport.fromJson(Map<String, dynamic> j) => SalesInvoiceReport(
        rows: ((j['rows'] as List?) ?? [])
            .map((e) => SalesInvoiceRow.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalInvoices: _i(j['totalInvoices']),
        totalSubtotal: _d(j['totalSubtotal']),
        totalDiscount: _d(j['totalDiscount']),
        grandTotal: _d(j['grandTotal']),
        currency: _s(j['currency']),
      );
}

/// One payment-method group (CollectionMethodLineDto).
class CollectionMethodLine {
  final String method;
  final int count;
  final double amount;

  CollectionMethodLine({required this.method, required this.count, required this.amount});

  factory CollectionMethodLine.fromJson(Map<String, dynamic> j) => CollectionMethodLine(
        method: _s(j['method']),
        count: _i(j['count']),
        amount: _d(j['amount']),
      );
}

/// Collection report (CollectionReportDto).
class CollectionReport {
  final List<CollectionMethodLine> byMethod;
  final int totalCount;
  final double totalCollected;
  final String currency;

  CollectionReport({
    required this.byMethod,
    required this.totalCount,
    required this.totalCollected,
    required this.currency,
  });

  factory CollectionReport.fromJson(Map<String, dynamic> j) => CollectionReport(
        byMethod: ((j['byMethod'] as List?) ?? [])
            .map((e) => CollectionMethodLine.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalCount: _i(j['totalCount']),
        totalCollected: _d(j['totalCollected']),
        currency: _s(j['currency']),
      );
}

/// One top-selling item row (TopItemRowDto).
class TopSellingItemRow {
  final String code;
  final String name;
  final int quantitySold;
  final double revenue;
  final String currency;

  TopSellingItemRow({
    required this.code,
    required this.name,
    required this.quantitySold,
    required this.revenue,
    required this.currency,
  });

  factory TopSellingItemRow.fromJson(Map<String, dynamic> j) => TopSellingItemRow(
        code: _s(j['code']),
        name: _s(j['name']),
        quantitySold: _i(j['quantitySold']),
        revenue: _d(j['revenue']),
        currency: _s(j['currency']),
      );
}
