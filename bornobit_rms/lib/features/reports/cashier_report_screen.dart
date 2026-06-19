import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'reports_models.dart';
import 'reports_providers.dart';
import 'widgets.dart';

const cashierReportRoute = '/operations/reports/cashier';

/// Operations → Reports → Cashier. Captured tenders over a date range, grouped
/// by the cashier who took them. Net = charges − refunds (money each cashier is
/// accountable for). Helps reconcile a cashier's drawer at shift end.
class CashierReportScreen extends ConsumerWidget {
  const CashierReportScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(cashierReportProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Cashier Report',
          subtitle: 'Captured tenders grouped by cashier (net of refunds).',
          actions: [RefreshAction(onPressed: () => ref.invalidate(cashierReportProvider))],
        ),
        const ReportsRangeSelector(),
        Expanded(
          child: AsyncStateView<CashierReport>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(cashierReportProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(CashierReport r) {
    final cur = r.currency;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Cashiers', value: count(r.rows.length), icon: Icons.people, tint: Bo.primaryTint),
            KpiCard(label: 'Transactions', value: count(r.totalTxns), icon: Icons.receipt_long, tint: Bo.infoSoft),
            KpiCard(label: 'Refunds', value: money(r.totalRefunds, cur), icon: Icons.undo, tint: Bo.dangerSoft),
            KpiCard(label: 'Net Collected', value: money(r.totalNet, cur), icon: Icons.account_balance_wallet, tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No captured tenders in this period.',
            columns: const [
              DataColumn(label: Text('Cashier')),
              DataColumn(label: Text('Txns'), numeric: true),
              DataColumn(label: Text('Charges'), numeric: true),
              DataColumn(label: Text('Refunds'), numeric: true),
              DataColumn(label: Text('Net'), numeric: true),
            ],
            rows: [
              for (final row in r.rows)
                DataRow(cells: [
                  DataCell(Text(row.cashier)),
                  DataCell(Text(count(row.txnCount))),
                  DataCell(Text(money(row.charges, cur))),
                  DataCell(Text(
                    row.refunds > 0 ? money(row.refunds, cur) : '—',
                    style: const TextStyle(color: Bo.danger),
                  )),
                  DataCell(Text(money(row.net, cur),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                ]),
              if (r.rows.isNotEmpty)
                DataRow(
                  color: WidgetStatePropertyAll(Bo.bgSoft),
                  cells: [
                    const DataCell(Text('Total', style: TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(count(r.totalTxns), style: const TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(r.totalCharges, cur), style: const TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(r.totalRefunds, cur),
                        style: const TextStyle(fontWeight: FontWeight.w800, color: Bo.danger))),
                    DataCell(Text(money(r.totalNet, cur),
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
