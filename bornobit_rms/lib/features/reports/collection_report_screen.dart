import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'reports_models.dart';
import 'reports_providers.dart';
import 'widgets.dart';

const collectionReportRoute = '/operations/reports/collection';

/// Operations → Reports → Collection. Money collected over a date range,
/// grouped by payment method. Mirrors the Blazor CollectionReport.razor page.
class CollectionReportScreen extends ConsumerWidget {
  const CollectionReportScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(collectionReportProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Collection Report',
          subtitle: 'Money collected over a date range, grouped by payment method.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(collectionReportProvider))],
        ),
        const ReportsRangeSelector(),
        Expanded(
          child: AsyncStateView<CollectionReport>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(collectionReportProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(CollectionReport r) {
    final cur = r.currency;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Payments', value: count(r.totalCount), icon: Icons.receipt_long, tint: Bo.primaryTint),
            KpiCard(label: 'Collected', value: money(r.totalCollected, cur), icon: Icons.account_balance_wallet, tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No payments collected in this period.',
            columns: const [
              DataColumn(label: Text('Payment method')),
              DataColumn(label: Text('Payments'), numeric: true),
              DataColumn(label: Text('Amount'), numeric: true),
            ],
            rows: [
              for (final row in r.byMethod)
                DataRow(cells: [
                  DataCell(Text(row.method)),
                  DataCell(Text(count(row.count))),
                  DataCell(Text(money(row.amount, cur),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                ]),
              if (r.byMethod.isNotEmpty)
                DataRow(
                  color: WidgetStatePropertyAll(Bo.bgSoft),
                  cells: [
                    const DataCell(Text('Total', style: TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(count(r.totalCount),
                        style: const TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(r.totalCollected, cur),
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
