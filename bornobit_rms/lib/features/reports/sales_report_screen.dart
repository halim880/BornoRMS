import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'reports_models.dart';
import 'reports_providers.dart';
import 'widgets.dart';

const salesReportRoute = '/operations/reports/sales';

/// Operations → Reports → Sales. Paid sales over a date range, per day.
/// Mirrors the Blazor SalesReport.razor page.
class SalesReportScreen extends ConsumerWidget {
  const SalesReportScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(salesReportProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Sales Report',
          subtitle: 'Paid sales over a date range, broken down per day.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(salesReportProvider))],
        ),
        const ReportsRangeSelector(),
        Expanded(
          child: AsyncStateView<SalesReport>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(salesReportProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(SalesReport r) {
    final cur = r.currency;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Orders', value: count(r.totalOrders), icon: Icons.receipt_long, tint: Bo.primaryTint),
            KpiCard(label: 'Subtotal', value: money(r.totalSubtotal, cur), icon: Icons.payments, tint: Bo.infoSoft),
            KpiCard(label: 'Discount', value: money(r.totalDiscount, cur), icon: Icons.sell, tint: Bo.dangerSoft),
            KpiCard(label: 'Total', value: money(r.grandTotal, cur), icon: Icons.account_balance_wallet, tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No paid sales in this period.',
            columns: const [
              DataColumn(label: Text('Date')),
              DataColumn(label: Text('Orders'), numeric: true),
              DataColumn(label: Text('Subtotal'), numeric: true),
              DataColumn(label: Text('Discount'), numeric: true),
              DataColumn(label: Text('Total'), numeric: true),
            ],
            rows: [
              for (final row in r.rows)
                DataRow(cells: [
                  DataCell(Text(shortDate(row.date))),
                  DataCell(Text(count(row.orderCount))),
                  DataCell(Text(money(row.subtotal, cur))),
                  DataCell(Text(
                    row.discount > 0 ? money(row.discount, cur) : '—',
                    style: const TextStyle(color: Bo.danger),
                  )),
                  DataCell(Text(money(row.total, cur),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                ]),
              if (r.rows.isNotEmpty)
                DataRow(
                  color: WidgetStatePropertyAll(Bo.bgSoft),
                  cells: [
                    const DataCell(Text('Total', style: TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(count(r.totalOrders),
                        style: const TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(r.totalSubtotal, cur),
                        style: const TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(r.totalDiscount, cur),
                        style: const TextStyle(fontWeight: FontWeight.w800, color: Bo.danger))),
                    DataCell(Text(money(r.grandTotal, cur),
                        style: const TextStyle(fontWeight: FontWeight.w800, color: Bo.success))),
                  ],
                ),
            ],
          ),
        ),
      ],
    );
  }
}
