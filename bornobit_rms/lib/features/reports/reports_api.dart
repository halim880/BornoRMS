import '../../core/api/staff_api.dart';
import '../../core/config/app_config.dart';
import 'reports_models.dart';

/// Typed wrappers over the /api/v1/staff/reports/* surface
/// (ReportsEndpoints.cs). Dates are sent as plain calendar dates `yyyy-MM-dd`
/// so a client-side UTC shift cannot move the window by a day (the server takes
/// `.Date`).
extension ReportsApi on StaffApi {
  String get _reportsBase => '${AppConfig.apiPrefix}/staff/reports';

  String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  Map<String, dynamic> _range(DateTime? from, DateTime? to) => {
        if (from != null) 'from': _dateOnly(from),
        if (to != null) 'to': _dateOnly(to),
      };

  Future<SalesReport> salesReport({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_reportsBase/sales', queryParameters: _range(from, to));
        return SalesReport.fromJson(res.data as Map<String, dynamic>);
      });

  Future<SalesInvoiceReport> salesInvoiceReport({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_reportsBase/sales-invoices', queryParameters: _range(from, to));
        return SalesInvoiceReport.fromJson(res.data as Map<String, dynamic>);
      });

  Future<CollectionReport> collectionReport({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_reportsBase/collection', queryParameters: _range(from, to));
        return CollectionReport.fromJson(res.data as Map<String, dynamic>);
      });

  Future<List<TopSellingItemRow>> topSellingItems({DateTime? from, DateTime? to, int top = 20}) =>
      client.guard(() async {
        final res = await client.dio.get('$_reportsBase/top-items',
            queryParameters: {..._range(from, to), 'top': top});
        return (res.data as List)
            .map((e) => TopSellingItemRow.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<List<CategorySalesRow>> categorySales({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_reportsBase/category-sales', queryParameters: _range(from, to));
        return (res.data as List)
            .map((e) => CategorySalesRow.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<CashierReport> cashierReport({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_reportsBase/cashier', queryParameters: _range(from, to));
        return CashierReport.fromJson(res.data as Map<String, dynamic>);
      });

  Future<PurchaseReport> purchaseReport({DateTime? from, DateTime? to}) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_reportsBase/purchases', queryParameters: _range(from, to));
        return PurchaseReport.fromJson(res.data as Map<String, dynamic>);
      });

  /// Point-in-time snapshot — no date range.
  Future<StockValuation> stockValuation() => client.guard(() async {
        final res = await client.dio.get('$_reportsBase/stock-valuation');
        return StockValuation.fromJson(res.data as Map<String, dynamic>);
      });
}
