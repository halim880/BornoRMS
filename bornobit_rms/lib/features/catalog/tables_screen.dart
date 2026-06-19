import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_form_dialog.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'catalog_api.dart';
import 'catalog_models.dart';
import 'catalog_providers.dart';

const tablesAdminRoute = '/inventory/tables';

const _pageSize = 12;

class TablesScreen extends ConsumerStatefulWidget {
  const TablesScreen({super.key});

  @override
  ConsumerState<TablesScreen> createState() => _TablesScreenState();
}

class _TablesScreenState extends ConsumerState<TablesScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(catalogTablesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Tables',
          subtitle:
              'Manage dining tables: table number and seating capacity. Inactive tables are hidden from order entry.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Table'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<CatalogTable>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(catalogTablesProvider),
            data: (tables) => _table(context, tables),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<CatalogTable> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No tables yet. Click 'New Table' to add one.",
      columns: const [
        DataColumn(label: Text('Table #')),
        DataColumn(label: Text('Capacity')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final t in rows)
          DataRow(cells: [
            DataCell(Text(t.tableNumber, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text('${t.capacity} seats')),
            DataCell(t.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, table: t),
              ),
              IconButton(
                tooltip: t.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(t.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: t.isActive ? Bo.success : Bo.textSubtle),
                onPressed: () => _toggleActive(context, t),
              ),
            ])),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} tables',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  Future<void> _toggleActive(BuildContext context, CatalogTable t) async {
    try {
      await ref.read(staffApiProvider).catalogSetTableActive(t.id, !t.isActive);
      ref.invalidate(catalogTablesProvider);
      if (context.mounted) {
        AppToast.show(context, t.isActive ? 'Table deactivated' : 'Table activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, {CatalogTable? table}) {
    final numberCtrl = TextEditingController(text: table?.tableNumber ?? '');
    final capacityCtrl = TextEditingController(text: '${table?.capacity ?? 4}');
    final isEdit = table != null;

    showDialog<bool>(
      context: context,
      builder: (_) => AppFormDialog(
        title: isEdit ? 'Edit Table' : 'New Table',
        icon: Icons.table_restaurant_outlined,
        onSave: () async {
          final api = ref.read(staffApiProvider);
          final number = numberCtrl.text.trim();
          final capacity = int.tryParse(capacityCtrl.text.trim()) ?? 0;
          if (isEdit) {
            await api.catalogUpdateTable(table.id, tableNumber: number, capacity: capacity);
          } else {
            await api.catalogCreateTable(tableNumber: number, capacity: capacity);
          }
          ref.invalidate(catalogTablesProvider);
          if (context.mounted) {
            AppToast.show(context, isEdit ? 'Table updated' : 'Table created');
          }
          return true;
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            FormField2(label: 'Table number', child: TextField(controller: numberCtrl)),
            FormField2(
                label: 'Capacity (seats)',
                child: TextField(
                    controller: capacityCtrl, keyboardType: TextInputType.number)),
          ],
        ),
      ),
    );
  }
}
