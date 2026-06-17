import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_colors.dart';
import 'pos_models.dart';
import 'pos_providers.dart';
import 'pos_section.dart';
import 'product_card.dart';

/// The catalog column: a categories panel (search + pills) above a products grid.
class CatalogView extends ConsumerWidget {
  const CatalogView({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final a = context.appColors;
    final catalogAsync = ref.watch(posCatalogProvider);

    return catalogAsync.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) =>
          Center(child: Text('Catalog: $e', style: TextStyle(color: a.textSecondary))),
      data: (catalog) {
        final selectedCat = ref.watch(posCategoryProvider);
        final search = ref.watch(posSearchProvider).toLowerCase();

        var products = catalog.products;
        if (selectedCat != null) {
          products = products.where((p) => p.categoryId == selectedCat).toList();
        }
        if (search.isNotEmpty) {
          products = products
              .where((p) =>
                  p.name.toLowerCase().contains(search) ||
                  p.code.toLowerCase().contains(search))
              .toList();
        }
        products = products.toList()
          ..sort((x, y) => x.displayOrder.compareTo(y.displayOrder));

        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _CategoriesPanel(categories: catalog.categories, selected: selectedCat),
            const SizedBox(height: 12),
            Expanded(
              child: PosPanel(
                padding: const EdgeInsets.all(12),
                child: products.isEmpty
                    ? Center(child: Text('No products', style: TextStyle(color: a.textTertiary)))
                    : GridView.builder(
                        padding: EdgeInsets.zero,
                        gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                          maxCrossAxisExtent: 170,
                          mainAxisExtent: 152,
                          crossAxisSpacing: 16,
                          mainAxisSpacing: 16,
                        ),
                        itemCount: products.length,
                        itemBuilder: (_, i) => ProductCard(
                          product: products[i],
                          availability: catalog.availability[products[i].id],
                        ),
                      ),
              ),
            ),
          ],
        );
      },
    );
  }
}

class _CategoriesPanel extends ConsumerStatefulWidget {
  final List<PosCategory> categories;
  final String? selected;
  const _CategoriesPanel({required this.categories, required this.selected});

  @override
  ConsumerState<_CategoriesPanel> createState() => _CategoriesPanelState();
}

class _CategoriesPanelState extends ConsumerState<_CategoriesPanel> {
  bool _searchOpen = false;
  final _searchCtrl = TextEditingController();
  final _searchFocus = FocusNode();

  @override
  void dispose() {
    _searchCtrl.dispose();
    _searchFocus.dispose();
    super.dispose();
  }

  void _toggleSearch() {
    setState(() => _searchOpen = !_searchOpen);
    if (_searchOpen) {
      _searchFocus.requestFocus();
    } else {
      _searchCtrl.clear();
      ref.read(posSearchProvider.notifier).state = '';
    }
  }

  @override
  Widget build(BuildContext context) {
    return PosPanel(
      padding: const EdgeInsets.fromLTRB(12, 12, 12, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (_searchOpen) ...[
            TextField(
              controller: _searchCtrl,
              focusNode: _searchFocus,
              decoration: InputDecoration(
                hintText: 'Search products',
                prefixIcon: const Icon(Icons.search),
                isDense: true,
                suffixIcon: IconButton(
                  icon: const Icon(Icons.close, size: 18),
                  tooltip: 'Close search',
                  onPressed: _toggleSearch,
                ),
              ),
              onChanged: (v) => ref.read(posSearchProvider.notifier).state = v,
            ),
            const SizedBox(height: 12),
          ],
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _SearchChip(active: _searchOpen, onTap: _toggleSearch),
              _CatPill(
                label: 'All',
                active: widget.selected == null,
                onTap: () => ref.read(posCategoryProvider.notifier).state = null,
              ),
              for (final cat in widget.categories)
                _CatPill(
                  label: cat.name,
                  active: widget.selected == cat.id,
                  onTap: () => ref.read(posCategoryProvider.notifier).state = cat.id,
                ),
            ],
          ),
        ],
      ),
    );
  }
}

/// A pill-shaped toggle that reveals/hides the product search box.
class _SearchChip extends StatelessWidget {
  final bool active;
  final VoidCallback onTap;
  const _SearchChip({required this.active, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    return Material(
      color: active ? a.accentTint : a.surfaceMuted,
      shape: StadiumBorder(
        side: BorderSide(color: active ? a.accentTint2 : a.border),
      ),
      child: InkWell(
        customBorder: const StadiumBorder(),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          child: Icon(
            active ? Icons.close : Icons.search,
            size: 17,
            color: active ? a.accentHover : a.textSecondary,
          ),
        ),
      ),
    );
  }
}

class _CatPill extends StatelessWidget {
  final String label;
  final bool active;
  final VoidCallback onTap;
  const _CatPill({required this.label, required this.active, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    return Material(
      color: active ? a.accentTint : a.surfaceMuted,
      shape: StadiumBorder(
        side: BorderSide(color: active ? a.accentTint2 : a.border),
      ),
      child: InkWell(
        customBorder: const StadiumBorder(),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (active) ...[
                Icon(Icons.check, size: 15, color: a.accentHover),
                const SizedBox(width: 6),
              ],
              Text(
                label,
                style: TextStyle(
                  color: active ? a.accentHover : a.textSecondary,
                  fontWeight: FontWeight.w600,
                  fontSize: 13,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
