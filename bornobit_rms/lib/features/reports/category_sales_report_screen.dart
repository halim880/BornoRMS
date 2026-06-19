import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'reports_models.dart';
import 'reports_providers.dart';
import 'widgets.dart';

const categorySalesReportRoute = '/operations/reports/category-sales';

/// Operations → Reports → Category Sales. Paid revenue grouped by product
/// category over a date range. Mirrors the dashboard's category pie as a table.
class CategorySalesReportScreen extends ConsumerWidget {
  const CategorySalesReportScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(categorySalesReportProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Category Sales',
          subtitle: 'Paid revenue grouped by product category.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(categorySalesReportProvider))],
        ),
        const ReportsRangeSelector(),
        Expanded(
          child: AsyncStateView<List<CategorySalesRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(categorySalesReportProvider),
            data: (rows) => _body(rows),
          ),
        ),
      ],
    );
  }

  Widget _body(List<CategorySalesRow> rows) {
    // The endpoint returns a bare list with no currency; use the console default.
    const cur = 'Tk';
    final totalRevenue = rows.fold<double>(0, (s, r) => s + r.revenue);
    final totalQty = rows.fold<int>(0, (s, r) => s + r.quantity);

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Categories', value: count(rows.length), icon: Icons.category, tint: Bo.primaryTint),
            KpiCard(label: 'Items Sold', value: count(totalQty), icon: Icons.shopping_bag, tint: Bo.infoSoft),
            KpiCard(label: 'Revenue', value: money(totalRevenue, cur), icon: Icons.payments, tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No paid sales in this period.',
            columns: const [
              DataColumn(label: Text('Category')),
              DataColumn(label: Text('Qty'), numeric: true),
              DataColumn(label: Text('Revenue'), numeric: true),
            ],
            rows: [
              for (final row in rows)
                DataRow(cells: [
                  DataCell(Text(row.category)),
                  DataCell(Text(count(row.quantity))),
                  DataCell(Text(money(row.revenue, cur),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                ]),
              if (rows.isNotEmpty)
                DataRow(
                  color: WidgetStatePropertyAll(Bo.bgSoft),
                  cells: [
                    const DataCell(Text('Total', style: TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(count(totalQty), style: const TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(totalRevenue, cur),
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
