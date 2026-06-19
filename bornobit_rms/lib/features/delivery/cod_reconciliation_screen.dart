import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'delivery_models.dart';
import 'delivery_providers.dart';

const codReconciliationRoute = '/logistics/cod';

/// Delivery → COD Reconciliation. Per-rider cash-on-delivery owed vs already
/// settled today, so the cashier can count a rider's cash before settling their
/// orders on the POS.
class CodReconciliationScreen extends ConsumerWidget {
  const CodReconciliationScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(codReconciliationProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'COD Reconciliation',
          subtitle: 'Cash each rider owes the till today. Settle each order on the POS once counted.',
          actions: [
            IconButton(
              tooltip: 'Refresh',
              icon: const Icon(Icons.refresh, color: Bo.textMuted),
              onPressed: () => ref.invalidate(codReconciliationProvider),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<RiderCodRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(codReconciliationProvider),
            data: (rows) => _table(rows),
          ),
        ),
      ],
    );
  }

  Widget _table(List<RiderCodRow> rows) {
    final totalOutstanding = rows.fold<double>(0, (s, r) => s + r.outstandingCod);
    final totalCollected = rows.fold<double>(0, (s, r) => s + r.collectedToday);

    return DataTableCard(
      emptyMessage: 'No deliveries handed out today.',
      columns: const [
        DataColumn(label: Text('Rider')),
        DataColumn(label: Text('Outstanding orders'), numeric: true),
        DataColumn(label: Text('COD owed'), numeric: true),
        DataColumn(label: Text('Collected today'), numeric: true),
      ],
      rows: [
        for (final r in rows)
          DataRow(cells: [
            DataCell(Text(r.riderName, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(count(r.outstandingCount))),
            DataCell(Text(
              r.outstandingCod > 0 ? money(r.outstandingCod, 'Tk') : '—',
              style: TextStyle(
                  fontWeight: FontWeight.w700,
                  color: r.outstandingCod > 0 ? Bo.warning : Bo.textMuted),
            )),
            DataCell(Text(money(r.collectedToday, 'Tk'), style: const TextStyle(color: Bo.success))),
          ]),
        if (rows.isNotEmpty)
          DataRow(
            color: WidgetStatePropertyAll(Bo.bgSoft),
            cells: [
              const DataCell(Text('Total', style: TextStyle(fontWeight: FontWeight.w800))),
              const DataCell(Text('')),
              DataCell(Text(money(totalOutstanding, 'Tk'),
                  style: const TextStyle(fontWeight: FontWeight.w800, color: Bo.warning))),
              DataCell(Text(money(totalCollected, 'Tk'),
                  style: const TextStyle(fontWeight: FontWeight.w800, color: Bo.success))),
            ],
          ),
      ],
    );
  }
}
