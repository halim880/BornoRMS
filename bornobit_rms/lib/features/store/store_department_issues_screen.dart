import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storeDepartmentIssuesRoute = '/store/reports/department-issues';

/// Store → Reports → Department Issues (consumption). What the store issued to
/// each department over a date range. Mirrors StoreDepartmentIssues.razor.
class StoreDepartmentIssuesScreen extends ConsumerWidget {
  const StoreDepartmentIssuesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeDepartmentIssuesProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Department Issues',
          subtitle: 'Stock consumption per department over a date range.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeDepartmentIssuesProvider))],
        ),
        const _RangeSelector(),
        Expanded(
          child: AsyncStateView<StoreDepartmentConsumption>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeDepartmentIssuesProvider),
            data: _body,
          ),
        ),
      ],
    );
  }

  Widget _body(StoreDepartmentConsumption r) {
    const cur = 'Tk';
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(
                label: 'Departments',
                value: count(r.rows.length),
                icon: Icons.apartment,
                tint: Bo.infoSoft),
            KpiCard(
                label: 'Total Value',
                value: money(r.grandTotalValue, cur),
                icon: Icons.payments,
                tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No issues in this period.',
            columns: const [
              DataColumn(label: Text('Department')),
              DataColumn(label: Text('Items'), numeric: true),
              DataColumn(label: Text('Total Qty'), numeric: true),
              DataColumn(label: Text('Total Value'), numeric: true),
            ],
            rows: [
              for (final row in r.rows)
                DataRow(cells: [
                  DataCell(Text(row.departmentName, style: const TextStyle(fontWeight: FontWeight.w600))),
                  DataCell(Text(row.items.length.toString())),
                  DataCell(Text(row.totalQtyBase.toString())),
                  DataCell(Text(money(row.totalValue, cur),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                ]),
            ],
          ),
        ),
      ],
    );
  }
}

class _RangeSelector extends ConsumerWidget {
  const _RangeSelector();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selected = ref.watch(storeReportRangeProvider);
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        children: [
          for (final r in DashboardRange.values)
            ChoiceChip(
              label: Text(r.label),
              selected: selected == r,
              onSelected: (_) => ref.read(storeReportRangeProvider.notifier).state = r,
            ),
        ],
      ),
    );
  }
}
