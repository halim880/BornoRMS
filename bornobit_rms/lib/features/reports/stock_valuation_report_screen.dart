import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'reports_models.dart';
import 'reports_providers.dart';
import 'widgets.dart';

const stockValuationReportRoute = '/operations/reports/stock-valuation';

/// Operations → Reports → Stock Valuation. A point-in-time snapshot of inventory
/// value (Σ QtyOnHand × moving-average cost), overall and by category. No date
/// range — it reflects stock as it stands right now.
class StockValuationReportScreen extends ConsumerWidget {
  const StockValuationReportScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(stockValuationReportProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Stock Valuation',
          subtitle: 'Current inventory value at moving-average cost, by category.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(stockValuationReportProvider))],
        ),
        Expanded(
          child: AsyncStateView<StockValuation>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockValuationReportProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(StockValuation r) {
    // Valuation is computed in the console's base currency.
    const cur = 'Tk';
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Categories', value: count(r.byCategory.length), icon: Icons.category, tint: Bo.primaryTint),
            KpiCard(label: 'Total Stock Value', value: money(r.totalValue, cur), icon: Icons.inventory, tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No active stock items to value.',
            columns: const [
              DataColumn(label: Text('Category')),
              DataColumn(label: Text('Value'), numeric: true),
            ],
            rows: [
              for (final row in r.byCategory)
                DataRow(cells: [
                  DataCell(Text(row.categoryName)),
                  DataCell(Text(money(row.value, cur),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                ]),
              if (r.byCategory.isNotEmpty)
                DataRow(
                  color: WidgetStatePropertyAll(Bo.bgSoft),
                  cells: [
                    const DataCell(Text('Total', style: TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(r.totalValue, cur),
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
