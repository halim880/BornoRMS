import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storeLedgerRoute = '/store/ledger';

/// Store → Movement Ledger. Recent stock movements across all items.
/// Mirrors StoreLedger.razor.
class StoreLedgerScreen extends ConsumerWidget {
  const StoreLedgerScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeLedgerProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Movement Ledger',
          subtitle: 'Recent stock movements (most recent 500).',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeLedgerProvider))],
        ),
        Expanded(
          child: AsyncStateView<StoreMovementLedger>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeLedgerProvider),
            data: _body,
          ),
        ),
      ],
    );
  }

  Widget _body(StoreMovementLedger ledger) {
    return DataTableCard(
      emptyMessage: 'No stock movements yet.',
      columns: const [
        DataColumn(label: Text('When')),
        DataColumn(label: Text('Item')),
        DataColumn(label: Text('Unit')),
        DataColumn(label: Text('Type')),
        DataColumn(label: Text('Qty'), numeric: true),
        DataColumn(label: Text('Unit Cost'), numeric: true),
        DataColumn(label: Text('Reason')),
      ],
      rows: [
        for (final r in ledger.rows)
          DataRow(cells: [
            DataCell(Text(dateTimeDmy(r.occurredAtUtc))),
            DataCell(Text(r.itemName, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(Text(r.unitCode)),
            DataCell(ToneChip(r.movementType, _moveTone(r.movementType))),
            DataCell(Text(r.qtyBase.toString(),
                style: TextStyle(
                    color: r.qtyBase < 0 ? Bo.danger : Bo.text, fontWeight: FontWeight.w600))),
            DataCell(Text(r.unitCost.toString())),
            DataCell(Text(r.reason ?? r.referenceType ?? '—')),
          ]),
      ],
    );
  }

  String _moveTone(String t) => switch (t) {
        'OpeningBalance' => 'neutral',
        'PurchaseIn' || 'AdjustmentIn' => 'success',
        'IssueOut' || 'AdjustmentOut' => 'info',
        'WastageOut' => 'danger',
        _ => 'neutral',
      };
}
