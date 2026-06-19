import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const stockMovementsRoute = '/stock/history';

/// Stock → History. The full stock ledger, newest first.
/// Mirrors the Blazor StockMovements.razor page.
class StockMovementsScreen extends ConsumerWidget {
  const StockMovementsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(stockMovementsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Stock History',
          subtitle: 'Every stock movement — purchases, consumption, wastage and adjustments.',
          actions: [
            IconButton(
              tooltip: 'Refresh',
              onPressed: () => ref.invalidate(stockMovementsProvider),
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<Paged<StockMovement>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockMovementsProvider),
            data: (paged) => _table(ref, paged),
          ),
        ),
      ],
    );
  }

  Widget _table(WidgetRef ref, Paged<StockMovement> paged) {
    return DataTableCard(
      emptyMessage: 'No stock movements recorded yet.',
      columns: const [
        DataColumn(label: Text('When')),
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Item')),
        DataColumn(label: Text('Type')),
        DataColumn(label: Text('Qty'), numeric: true),
        DataColumn(label: Text('Unit Cost'), numeric: true),
        DataColumn(label: Text('Reason')),
        DataColumn(label: Text('By')),
      ],
      rows: [
        for (final m in paged.items)
          DataRow(cells: [
            DataCell(Text(dateTimeDmy(m.occurredAtUtc))),
            DataCell(Text(m.itemCode, style: const TextStyle(color: Bo.textSubtle))),
            DataCell(Text(m.itemName, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(ToneChip(stockMovementLabel(m.movementType), stockMovementTone(m.movementType))),
            DataCell(Text('${m.qtyBase} ${m.unitCode}',
                style: TextStyle(
                    fontWeight: FontWeight.w600,
                    color: m.qtyBase < 0 ? Bo.danger : Bo.success))),
            DataCell(Text(money(m.unitCost, 'Tk'))),
            DataCell(Text(m.reason ?? '—', style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(m.createdBy ?? '—', style: const TextStyle(color: Bo.textSubtle))),
          ]),
      ],
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} movements',
        onPage: (pg) => ref.read(stockMovementsPageProvider.notifier).state = pg,
      ),
    );
  }
}
