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

const stockItemsRoute = '/stock/items';

/// Stock → Items. Server-paged stock-item grid with search + create/edit.
/// Mirrors the Blazor StockItems.razor page.
class StockItemsScreen extends ConsumerStatefulWidget {
  const StockItemsScreen({super.key});

  @override
  ConsumerState<StockItemsScreen> createState() => _StockItemsScreenState();
}

class _StockItemsScreenState extends ConsumerState<StockItemsScreen> {
  final _searchCtrl = TextEditingController();

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  void _setFilter(StockItemsFilter f) =>
      ref.read(stockItemsFilterProvider.notifier).state = f;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(stockItemsProvider);
    final filter = ref.watch(stockItemsFilterProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Stock Items',
          subtitle: 'Raw ingredients and finished goods tracked in inventory.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Item'),
            ),
          ],
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
          child: Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _searchCtrl,
                  decoration: const InputDecoration(
                    hintText: 'Search by name or code…',
                    prefixIcon: Icon(Icons.search),
                    isDense: true,
                  ),
                  onSubmitted: (v) => _setFilter(filter.copyWith(search: v.trim(), page: 1)),
                ),
              ),
              const SizedBox(width: 12),
              FilterChip(
                label: const Text('Low stock'),
                selected: filter.lowStockOnly,
                onSelected: (v) => _setFilter(filter.copyWith(lowStockOnly: v, page: 1)),
              ),
            ],
          ),
        ),
        Expanded(
          child: AsyncStateView<Paged<StockItem>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockItemsProvider),
            data: (paged) => _table(context, paged, filter),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, Paged<StockItem> paged, StockItemsFilter filter) {
    return DataTableCard(
      emptyMessage: 'No stock items match.',
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Category')),
        DataColumn(label: Text('Type')),
        DataColumn(label: Text('On Hand'), numeric: true),
        DataColumn(label: Text('Reorder'), numeric: true),
        DataColumn(label: Text('Avg Cost'), numeric: true),
        DataColumn(label: Text('Value'), numeric: true),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final i in paged.items)
          DataRow(cells: [
            DataCell(Text(i.code, style: const TextStyle(color: Bo.textSubtle))),
            DataCell(Text(i.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(i.categoryName)),
            DataCell(Text(i.itemTypeLabel, style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text('${i.qtyOnHand} ${i.unitCode}',
                style: TextStyle(
                    fontWeight: FontWeight.w600,
                    color: i.isLowStock ? Bo.danger : Bo.text))),
            DataCell(Text('${i.reorderLevel}')),
            DataCell(Text(money(i.avgCost, i.currency))),
            DataCell(Text(money(i.stockValue, i.currency))),
            DataCell(i.isLowStock
                ? const ToneChip('Low', 'warning')
                : (i.isActive ? const ToneChip('Active', 'success') : const ToneChip('Inactive', 'neutral'))),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, item: i),
              ),
              IconButton(
                tooltip: i.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(i.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: i.isActive ? Bo.success : Bo.textSubtle),
                onPressed: () => _toggleActive(context, i),
              ),
            ])),
          ]),
      ],
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} items',
        onPage: (p) => _setFilter(filter.copyWith(page: p)),
      ),
    );
  }

  Future<void> _toggleActive(BuildContext context, StockItem i) async {
    try {
      await ref.read(staffApiProvider).stockSetItemActive(i.id, !i.isActive);
      ref.invalidate(stockItemsProvider);
      if (context.mounted) {
        AppToast.show(context, i.isActive ? 'Item deactivated' : 'Item activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, {StockItem? item}) {
    final isEdit = item != null;
    final codeCtrl = TextEditingController(text: item?.code ?? '');
    final nameCtrl = TextEditingController(text: item?.name ?? '');
    final banglaCtrl = TextEditingController(text: item?.banglaName ?? '');
    final reorderLevelCtrl = TextEditingController(text: '${item?.reorderLevel ?? 0}');
    final reorderQtyCtrl = TextEditingController(text: '${item?.reorderQty ?? 0}');
    String? categoryId = item?.inventoryCategoryId;
    String? unitId = item?.baseUnitId;
    int itemType = item?.itemType ?? 1;
    bool perishable = item?.isPerishable ?? false;

    showDialog<bool>(
      context: context,
      builder: (_) => Consumer(builder: (context, ref, _) {
        final cats = ref.watch(stockCategoriesProvider);
        final units = ref.watch(stockUnitsProvider);
        return StatefulBuilder(builder: (context, setLocal) {
          return AppFormDialog(
            title: isEdit ? 'Edit Stock Item' : 'New Stock Item',
            icon: Icons.inventory_2,
            onSave: () async {
              final api = ref.read(staffApiProvider);
              if (categoryId == null || unitId == null) {
                throw 'Category and base unit are required.';
              }
              final body = {
                'code': codeCtrl.text.trim(),
                'name': nameCtrl.text.trim(),
                if (banglaCtrl.text.trim().isNotEmpty) 'banglaName': banglaCtrl.text.trim(),
                'inventoryCategoryId': categoryId,
                'itemType': itemType,
                'baseUnitId': unitId,
                'reorderLevel': double.tryParse(reorderLevelCtrl.text.trim()) ?? 0,
                'reorderQty': double.tryParse(reorderQtyCtrl.text.trim()) ?? 0,
                'isPerishable': perishable,
              };
              if (isEdit) {
                await api.stockUpdateItem(item.id, body);
              } else {
                await api.stockCreateItem(body);
              }
              ref.invalidate(stockItemsProvider);
              if (context.mounted) {
                AppToast.show(context, isEdit ? 'Item updated' : 'Item created');
              }
              return true;
            },
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                FormField2(label: 'Code', child: TextField(controller: codeCtrl)),
                FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
                FormField2(label: 'Bangla name (optional)', child: TextField(controller: banglaCtrl)),
                FormField2(
                  label: 'Category',
                  child: cats.when(
                    loading: () => const LinearProgressIndicator(),
                    error: (e, _) => Text('$e', style: const TextStyle(color: Bo.danger)),
                    data: (list) => DropdownButtonFormField<String>(
                      initialValue: categoryId,
                      isExpanded: true,
                      items: [
                        for (final c in list)
                          DropdownMenuItem(value: c.id, child: Text(c.name)),
                      ],
                      onChanged: (v) => setLocal(() => categoryId = v),
                    ),
                  ),
                ),
                FormField2(
                  label: 'Base unit',
                  child: units.when(
                    loading: () => const LinearProgressIndicator(),
                    error: (e, _) => Text('$e', style: const TextStyle(color: Bo.danger)),
                    data: (list) => DropdownButtonFormField<String>(
                      initialValue: unitId,
                      isExpanded: true,
                      items: [
                        for (final u in list)
                          DropdownMenuItem(value: u.id, child: Text('${u.name} (${u.code})')),
                      ],
                      onChanged: (v) => setLocal(() => unitId = v),
                    ),
                  ),
                ),
                FormField2(
                  label: 'Item type',
                  child: DropdownButtonFormField<int>(
                    initialValue: itemType,
                    isExpanded: true,
                    items: const [
                      DropdownMenuItem(value: 1, child: Text('Ingredient')),
                      DropdownMenuItem(value: 2, child: Text('Finished Good')),
                    ],
                    onChanged: (v) => setLocal(() => itemType = v ?? 1),
                  ),
                ),
                Row(children: [
                  Expanded(
                    child: FormField2(
                      label: 'Reorder level',
                      child: TextField(controller: reorderLevelCtrl, keyboardType: TextInputType.number),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FormField2(
                      label: 'Reorder qty',
                      child: TextField(controller: reorderQtyCtrl, keyboardType: TextInputType.number),
                    ),
                  ),
                ]),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Perishable'),
                  value: perishable,
                  onChanged: (v) => setLocal(() => perishable = v),
                ),
              ],
            ),
          );
        });
      }),
    );
  }
}
