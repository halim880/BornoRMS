import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'reports_models.dart';
import 'reports_providers.dart';
import 'widgets.dart';

const purchaseReportRoute = '/operations/reports/purchases';

/// Operations → Reports → Purchases. Posted goods receipts over a date range,
/// grouped by supplier — what we actually bought (drafts excluded, since only
/// posted GRNs raise stock and Accounts Payable).
class PurchaseReportScreen extends ConsumerWidget {
  const PurchaseReportScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(purchaseReportProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Purchase Report',
          subtitle: 'Posted goods receipts grouped by supplier.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(purchaseReportProvider))],
        ),
        const ReportsRangeSelector(),
        Expanded(
          child: AsyncStateView<PurchaseReport>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(purchaseReportProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(PurchaseReport r) {
    final cur = r.currency;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Suppliers', value: count(r.rows.length), icon: Icons.people_alt, tint: Bo.primaryTint),
            KpiCard(label: 'Receipts', value: count(r.totalGrns), icon: Icons.inventory_2, tint: Bo.infoSoft),
            KpiCard(label: 'Total Purchased', value: money(r.grandTotal, cur), icon: Icons.shopping_cart, tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No posted goods receipts in this period.',
            columns: const [
              DataColumn(label: Text('Supplier')),
              DataColumn(label: Text('Receipts'), numeric: true),
              DataColumn(label: Text('Amount'), numeric: true),
            ],
            rows: [
              for (final row in r.rows)
                DataRow(cells: [
                  DataCell(Text(row.supplierName)),
                  DataCell(Text(count(row.grnCount))),
                  DataCell(Text(money(row.subtotal, cur),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                ]),
              if (r.rows.isNotEmpty)
                DataRow(
                  color: WidgetStatePropertyAll(Bo.bgSoft),
                  cells: [
                    const DataCell(Text('Total', style: TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(count(r.totalGrns), style: const TextStyle(fontWeight: FontWeight.w800))),
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
