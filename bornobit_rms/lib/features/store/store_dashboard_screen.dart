import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storeDashboardRoute = '/store/dashboard';

/// Store → Dashboard. KPI grid (stock value, active items, low stock, drafts) +
/// a low-stock table. Mirrors the Blazor StoreDashboard.razor page.
class StoreDashboardScreen extends ConsumerWidget {
  const StoreDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeDashboardProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Store Dashboard',
          subtitle: 'Stock value, low-stock items and pending drafts.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeDashboardProvider))],
        ),
        Expanded(
          child: AsyncStateView<StoreDashboard>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeDashboardProvider),
            data: _body,
          ),
        ),
      ],
    );
  }

  Widget _body(StoreDashboard d) {
    final cur = d.summary.currency;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
          child: KpiGrid(children: [
            KpiCard(
                label: 'Stock Value',
                value: money(d.summary.totalStockValue, cur),
                icon: Icons.inventory_2,
                tint: Bo.primaryTint),
            KpiCard(
                label: 'Active Items',
                value: count(d.summary.activeItemCount),
                icon: Icons.category,
                tint: Bo.infoSoft),
            KpiCard(
                label: 'Low Stock',
                value: count(d.summary.lowStockItemCount),
                icon: Icons.warning_amber,
                tint: Bo.dangerSoft),
            KpiCard(
                label: 'Draft GRN / Issues',
                value: '${d.summary.draftGrnCount} / ${d.summary.draftIssueCount}',
                icon: Icons.drafts,
                tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No low-stock items. Stock levels are healthy.',
            columns: const [
              DataColumn(label: Text('Code')),
              DataColumn(label: Text('Item')),
              DataColumn(label: Text('Unit')),
              DataColumn(label: Text('On Hand'), numeric: true),
              DataColumn(label: Text('Reorder At'), numeric: true),
              DataColumn(label: Text('Reorder Qty'), numeric: true),
              DataColumn(label: Text('Value'), numeric: true),
            ],
            rows: [
              for (final r in d.lowStock)
                DataRow(cells: [
                  DataCell(Text(r.code)),
                  DataCell(Text(r.name, style: const TextStyle(fontWeight: FontWeight.w600))),
                  DataCell(Text(r.unitCode)),
                  DataCell(Text(r.qtyOnHand.toString(),
                      style: const TextStyle(color: Bo.danger, fontWeight: FontWeight.w700))),
                  DataCell(Text(r.reorderLevel.toString())),
                  DataCell(Text(r.reorderQty.toString())),
                  DataCell(Text(money(r.stockValue, cur))),
                ]),
            ],
          ),
        ),
      ],
    );
  }
}
