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

const productsRoute = '/inventory/products';

const _pageSize = 12;

class ProductsScreen extends ConsumerStatefulWidget {
  const ProductsScreen({super.key});

  @override
  ConsumerState<ProductsScreen> createState() => _ProductsScreenState();
}

class _ProductsScreenState extends ConsumerState<ProductsScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(catalogProductsProvider);
    final categories = ref.watch(catalogCategoriesProvider).valueOrNull ?? const [];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Products',
          subtitle: 'Manage products: image, English & Bangla name, category, code and price.',
          actions: [
            FilledButton.icon(
              onPressed: categories.isEmpty ? null : () => _openForm(context, categories),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Product'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<CatalogProduct>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(catalogProductsProvider),
            data: (products) => _table(context, products, categories),
          ),
        ),
      ],
    );
  }

  Widget _table(
      BuildContext context, List<CatalogProduct> all, List<CatalogCategory> categories) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No products yet. Click 'New Product' to add one.",
      columns: const [
        DataColumn(label: Text('Order')),
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Bangla name')),
        DataColumn(label: Text('Category')),
        DataColumn(label: Text('Price')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final p in rows)
          DataRow(cells: [
            DataCell(Text('${p.displayOrder}', style: const TextStyle(color: Bo.textSubtle))),
            DataCell(Text(p.code, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(_nameCell(p)),
            DataCell(Text(p.banglaName?.isNotEmpty == true ? p.banglaName! : '—')),
            DataCell(Text(p.categoryName, style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(p.hasVariants
                ? 'from ${money(p.minPrice, p.currency)}'
                : money(p.price, p.currency))),
            DataCell(p.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, categories, product: p),
              ),
              IconButton(
                tooltip: p.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(p.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: p.isActive ? Bo.success : Bo.textSubtle),
                onPressed: () => _toggleActive(context, p),
              ),
            ])),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} products',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  Widget _nameCell(CatalogProduct p) {
    final badges = <Widget>[
      if (p.isCombo) const _Badge('Combo'),
      if (p.hasOptions) _Badge('${p.optionGroupCount} option${p.optionGroupCount == 1 ? '' : 's'}'),
    ];
    return Row(mainAxisSize: MainAxisSize.min, children: [
      Text(p.name, style: const TextStyle(fontWeight: FontWeight.w700)),
      for (final b in badges) ...[const SizedBox(width: 6), b],
    ]);
  }

  Future<void> _toggleActive(BuildContext context, CatalogProduct p) async {
    try {
      await ref.read(staffApiProvider).catalogSetProductActive(p.id, !p.isActive);
      ref.invalidate(catalogProductsProvider);
      if (context.mounted) {
        AppToast.show(context, p.isActive ? 'Product deactivated' : 'Product activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, List<CatalogCategory> categories,
      {CatalogProduct? product}) {
    final codeCtrl = TextEditingController(text: product?.code ?? '');
    final nameCtrl = TextEditingController(text: product?.name ?? '');
    final banglaCtrl = TextEditingController(text: product?.banglaName ?? '');
    final priceCtrl = TextEditingController(text: '${product?.price ?? 0}');
    final descCtrl = TextEditingController(text: product?.description ?? '');
    final imageCtrl = TextEditingController(text: product?.imagePath ?? '');
    final orderCtrl = TextEditingController(text: '${product?.displayOrder ?? 0}');
    final isEdit = product != null;
    var categoryId = product?.productCategoryId ??
        (categories.isNotEmpty ? categories.first.id : '');

    showDialog<bool>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (context, setLocal) => AppFormDialog(
          title: isEdit ? 'Edit Product' : 'New Product',
          icon: Icons.fastfood_outlined,
          onSave: () async {
            final api = ref.read(staffApiProvider);
            final code = codeCtrl.text.trim();
            final name = nameCtrl.text.trim();
            final bangla = banglaCtrl.text.trim().isEmpty ? null : banglaCtrl.text.trim();
            final price = double.tryParse(priceCtrl.text.trim()) ?? 0;
            final desc = descCtrl.text.trim().isEmpty ? null : descCtrl.text.trim();
            final image = imageCtrl.text.trim().isEmpty ? null : imageCtrl.text.trim();
            final order = int.tryParse(orderCtrl.text.trim()) ?? 0;
            if (isEdit) {
              await api.catalogUpdateProduct(product.id,
                  productCategoryId: categoryId,
                  code: code,
                  name: name,
                  banglaName: bangla,
                  price: price,
                  description: desc,
                  imagePath: image,
                  displayOrder: order);
            } else {
              await api.catalogCreateProduct(
                  productCategoryId: categoryId,
                  code: code,
                  name: name,
                  banglaName: bangla,
                  price: price,
                  description: desc,
                  imagePath: image,
                  displayOrder: order);
            }
            ref.invalidate(catalogProductsProvider);
            if (context.mounted) {
              AppToast.show(context, isEdit ? 'Product updated' : 'Product created');
            }
            return true;
          },
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormField2(
                label: 'Category',
                child: DropdownButtonFormField<String>(
                  initialValue: categoryId.isEmpty ? null : categoryId,
                  items: [
                    for (final c in categories)
                      DropdownMenuItem(value: c.id, child: Text(c.name)),
                  ],
                  onChanged: (v) => setLocal(() => categoryId = v ?? categoryId),
                ),
              ),
              FormField2(label: 'Code', child: TextField(controller: codeCtrl)),
              FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
              FormField2(label: 'Bangla name', child: TextField(controller: banglaCtrl)),
              FormField2(
                  label: 'Price',
                  child: TextField(
                      controller: priceCtrl,
                      keyboardType: const TextInputType.numberWithOptions(decimal: true))),
              FormField2(
                  label: 'Image path (optional)',
                  child: TextField(controller: imageCtrl)),
              FormField2(
                  label: 'Description',
                  child: TextField(controller: descCtrl, minLines: 1, maxLines: 3)),
              FormField2(
                  label: 'Display order',
                  child: TextField(
                      controller: orderCtrl, keyboardType: TextInputType.number)),
            ],
          ),
        ),
      ),
    );
  }
}

class _Badge extends StatelessWidget {
  final String text;
  const _Badge(this.text);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
      decoration: BoxDecoration(color: Bo.bgSoft, borderRadius: BorderRadius.circular(Bo.radiusSm)),
      child: Text(text, style: const TextStyle(fontSize: 11, color: Bo.textMuted)),
    );
  }
}
