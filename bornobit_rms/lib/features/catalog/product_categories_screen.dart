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

const productCategoriesRoute = '/inventory/categories';

const _pageSize = 12;

class ProductCategoriesScreen extends ConsumerStatefulWidget {
  const ProductCategoriesScreen({super.key});

  @override
  ConsumerState<ProductCategoriesScreen> createState() => _ProductCategoriesScreenState();
}

class _ProductCategoriesScreenState extends ConsumerState<ProductCategoriesScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(catalogCategoriesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Product Categories',
          subtitle: 'Group products into categories. Display order controls how they sort.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Category'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<CatalogCategory>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(catalogCategoriesProvider),
            data: (categories) => _table(context, categories),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<CatalogCategory> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No product categories yet. Click 'New Category' to add one.",
      columns: const [
        DataColumn(label: Text('Order')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Description')),
        DataColumn(label: Text('Tax %')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final c in rows)
          DataRow(cells: [
            DataCell(Text('${c.displayOrder}', style: const TextStyle(color: Bo.textSubtle))),
            DataCell(Text(c.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(c.description?.isNotEmpty == true ? c.description! : '—',
                style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(c.taxRatePercent == null ? '—' : '${c.taxRatePercent}')),
            DataCell(c.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, category: c),
              ),
              IconButton(
                tooltip: c.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(c.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: c.isActive ? Bo.success : Bo.textSubtle),
                onPressed: () => _toggleActive(context, c),
              ),
            ])),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} categories',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  Future<void> _toggleActive(BuildContext context, CatalogCategory c) async {
    try {
      await ref.read(staffApiProvider).catalogSetCategoryActive(c.id, !c.isActive);
      ref.invalidate(catalogCategoriesProvider);
      if (context.mounted) {
        AppToast.show(context, c.isActive ? 'Category deactivated' : 'Category activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, {CatalogCategory? category}) {
    final nameCtrl = TextEditingController(text: category?.name ?? '');
    final descCtrl = TextEditingController(text: category?.description ?? '');
    final orderCtrl = TextEditingController(text: '${category?.displayOrder ?? 0}');
    final taxCtrl = TextEditingController(
        text: category?.taxRatePercent == null ? '' : '${category!.taxRatePercent}');
    final isEdit = category != null;

    showDialog<bool>(
      context: context,
      builder: (_) => AppFormDialog(
        title: isEdit ? 'Edit Category' : 'New Category',
        icon: Icons.category_outlined,
        onSave: () async {
          final api = ref.read(staffApiProvider);
          final name = nameCtrl.text.trim();
          final order = int.tryParse(orderCtrl.text.trim()) ?? 0;
          final tax = double.tryParse(taxCtrl.text.trim());
          final desc = descCtrl.text.trim().isEmpty ? null : descCtrl.text.trim();
          if (isEdit) {
            await api.catalogUpdateCategory(category.id,
                name: name, description: desc, displayOrder: order, taxRatePercent: tax);
          } else {
            await api.catalogCreateCategory(
                name: name, description: desc, displayOrder: order, taxRatePercent: tax);
          }
          ref.invalidate(catalogCategoriesProvider);
          if (context.mounted) {
            AppToast.show(context, isEdit ? 'Category updated' : 'Category created');
          }
          return true;
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
            FormField2(
                label: 'Description',
                child: TextField(controller: descCtrl, minLines: 1, maxLines: 3)),
            FormField2(
                label: 'Display order',
                child: TextField(
                    controller: orderCtrl, keyboardType: TextInputType.number)),
            FormField2(
                label: 'Tax rate % (optional)',
                child: TextField(
                    controller: taxCtrl,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true))),
          ],
        ),
      ),
    );
  }
}
