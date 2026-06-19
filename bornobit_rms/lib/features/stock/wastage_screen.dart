import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_form_dialog.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'stock_api.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const wastageRoute = '/stock/wastage';

/// Stock → Wastage. Records spoilage write-offs and physical-count adjustments,
/// and lists past WastageOut / Adjustment movements. Mirrors the Blazor
/// Wastage.razor page.
class WastageScreen extends ConsumerWidget {
  const WastageScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(stockMovementsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Wastage & Adjustments',
          subtitle: 'Write off spoilage or reconcile to a physical count.',
          actions: [
            OutlinedButton.icon(
              onPressed: () => _openAdjust(context, ref),
              icon: const Icon(Icons.tune, size: 18),
              label: const Text('Adjust'),
            ),
            const SizedBox(width: 8),
            FilledButton.icon(
              onPressed: () => _openWastage(context, ref),
              icon: const Icon(Icons.delete_outline, size: 18),
              label: const Text('Record Wastage'),
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
    // Show only wastage + adjustment rows on this page.
    final rows = paged.items.where((m) => m.movementType >= 3 && m.movementType <= 5).toList();
    return DataTableCard(
      emptyMessage: 'No wastage or adjustments recorded.',
      columns: const [
        DataColumn(label: Text('When')),
        DataColumn(label: Text('Item')),
        DataColumn(label: Text('Type')),
        DataColumn(label: Text('Qty'), numeric: true),
        DataColumn(label: Text('Reason')),
        DataColumn(label: Text('By')),
      ],
      rows: [
        for (final m in rows)
          DataRow(cells: [
            DataCell(Text(dateTimeDmy(m.occurredAtUtc))),
            DataCell(Text(m.itemName, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(ToneChip(stockMovementLabel(m.movementType), stockMovementTone(m.movementType))),
            DataCell(Text('${m.qtyBase} ${m.unitCode}',
                style: TextStyle(color: m.qtyBase < 0 ? Bo.danger : Bo.success))),
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

  void _openWastage(BuildContext context, WidgetRef ref) {
    String? itemId;
    final qtyCtrl = TextEditingController();
    final reasonCtrl = TextEditingController();

    showDialog<bool>(
      context: context,
      builder: (_) => Consumer(builder: (context, ref, _) {
        final items = ref.watch(stockAllItemsProvider);
        return StatefulBuilder(builder: (context, setLocal) {
          return AppFormDialog(
            title: 'Record Wastage',
            icon: Icons.delete_outline,
            onSave: () async {
              if (itemId == null) throw 'Pick an item.';
              final qty = double.tryParse(qtyCtrl.text.trim()) ?? 0;
              if (qty <= 0) throw 'Quantity must be greater than zero.';
              if (reasonCtrl.text.trim().isEmpty) throw 'A reason is required.';
              await ref.read(staffApiProvider).stockRecordWastage(
                  itemId: itemId!, qtyBase: qty, reason: reasonCtrl.text.trim());
              ref.invalidate(stockMovementsProvider);
              if (context.mounted) AppToast.show(context, 'Wastage recorded');
              return true;
            },
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                FormField2(label: 'Item', child: _itemPicker(items, itemId, (v) => setLocal(() => itemId = v))),
                FormField2(
                    label: 'Quantity (base unit)',
                    child: TextField(
                        controller: qtyCtrl,
                        keyboardType: const TextInputType.numberWithOptions(decimal: true))),
                FormField2(
                    label: 'Reason',
                    child: TextField(controller: reasonCtrl, minLines: 1, maxLines: 3)),
              ],
            ),
          );
        });
      }),
    );
  }

  void _openAdjust(BuildContext context, WidgetRef ref) {
    String? itemId;
    final qtyCtrl = TextEditingController();
    final reasonCtrl = TextEditingController();

    showDialog<bool>(
      context: context,
      builder: (_) => Consumer(builder: (context, ref, _) {
        final items = ref.watch(stockAllItemsProvider);
        return StatefulBuilder(builder: (context, setLocal) {
          return AppFormDialog(
            title: 'Adjust Stock',
            icon: Icons.tune,
            onSave: () async {
              if (itemId == null) throw 'Pick an item.';
              final qty = double.tryParse(qtyCtrl.text.trim());
              if (qty == null || qty < 0) throw 'Enter the counted quantity (0 or more).';
              await ref.read(staffApiProvider).stockAdjust(
                  itemId: itemId!,
                  countedQtyBase: qty,
                  reason: reasonCtrl.text.trim().isEmpty ? null : reasonCtrl.text.trim());
              ref.invalidate(stockMovementsProvider);
              if (context.mounted) AppToast.show(context, 'Stock adjusted');
              return true;
            },
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                FormField2(label: 'Item', child: _itemPicker(items, itemId, (v) => setLocal(() => itemId = v))),
                FormField2(
                    label: 'Counted quantity (base unit)',
                    child: TextField(
                        controller: qtyCtrl,
                        keyboardType: const TextInputType.numberWithOptions(decimal: true))),
                FormField2(
                    label: 'Reason (optional)',
                    child: TextField(controller: reasonCtrl, minLines: 1, maxLines: 3)),
              ],
            ),
          );
        });
      }),
    );
  }

  Widget _itemPicker(
      AsyncValue<List<StockItem>> items, String? selected, ValueChanged<String?> onChanged) {
    return items.when(
      loading: () => const LinearProgressIndicator(),
      error: (e, _) => Text('$e', style: const TextStyle(color: Bo.danger)),
      data: (list) => DropdownButtonFormField<String>(
        initialValue: selected,
        isExpanded: true,
        items: [
          for (final i in list)
            DropdownMenuItem(value: i.id, child: Text('${i.name} (${i.qtyOnHand} ${i.unitCode})')),
        ],
        onChanged: onChanged,
      ),
    );
  }
}
