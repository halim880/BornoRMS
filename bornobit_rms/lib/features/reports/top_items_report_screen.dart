import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'reports_models.dart';
import 'reports_providers.dart';
import 'widgets.dart';

const topItemsReportRoute = '/operations/reports/top-items';

/// Operations → Reports → Most Selling Items. Top menu items by quantity sold
/// over a date range, from paid orders. Mirrors the Blazor TopSellingItems.razor page.
class TopItemsReportScreen extends ConsumerWidget {
  const TopItemsReportScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(topItemsReportProvider);
    final top = ref.watch(topItemsCountProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Most Selling Items',
          subtitle: 'Top menu items by quantity sold over a date range, from paid orders.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(topItemsReportProvider))],
        ),
        const ReportsRangeSelector(),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
          child: Row(
            children: [
              const Text('Top N', style: TextStyle(color: Bo.textMuted, fontSize: 13)),
              const SizedBox(width: 12),
              DropdownButton<int>(
                value: top,
                items: const [
                  DropdownMenuItem(value: 10, child: Text('10')),
                  DropdownMenuItem(value: 20, child: Text('20')),
                  DropdownMenuItem(value: 50, child: Text('50')),
                  DropdownMenuItem(value: 100, child: Text('100')),
                ],
                onChanged: (v) {
                  if (v != null) ref.read(topItemsCountProvider.notifier).state = v;
                },
              ),
            ],
          ),
        ),
        Expanded(
          child: AsyncStateView<List<TopSellingItemRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(topItemsReportProvider),
            data: (rows) => _body(rows),
          ),
        ),
      ],
    );
  }

  Widget _body(List<TopSellingItemRow> rows) {
    final cur = rows.isEmpty ? '' : rows.first.currency;
    var rank = 0;
    return DataTableCard(
      emptyMessage: 'No items sold in this period.',
      columns: const [
        DataColumn(label: Text('#'), numeric: true),
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Item')),
        DataColumn(label: Text('Qty sold'), numeric: true),
        DataColumn(label: Text('Revenue'), numeric: true),
      ],
      rows: [
        for (final row in rows)
          DataRow(cells: [
            DataCell(Text('${++rank}', style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(row.code)),
            DataCell(Text(row.name)),
            DataCell(Text(count(row.quantitySold),
                style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(money(row.revenue, cur))),
          ]),
      ],
    );
  }
}
