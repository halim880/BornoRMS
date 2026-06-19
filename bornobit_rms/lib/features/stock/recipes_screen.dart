import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const recipesRoute = '/stock/recipes';

/// Stock → Recipes. Recipe-based products and their ingredient counts.
/// Tapping a row shows the full BOM (read-only) in a dialog.
/// Mirrors the Blazor Recipes.razor page.
class RecipesScreen extends ConsumerStatefulWidget {
  const RecipesScreen({super.key});

  @override
  ConsumerState<RecipesScreen> createState() => _RecipesScreenState();
}

class _RecipesScreenState extends ConsumerState<RecipesScreen> {
  static const _pageSize = 15;
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(stockRecipesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Recipes',
          subtitle: 'Bills of materials for recipe-based products.',
          actions: [
            IconButton(
              tooltip: 'Refresh',
              onPressed: () => ref.invalidate(stockRecipesProvider),
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<RecipeListRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockRecipesProvider),
            data: (list) => _table(context, list),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<RecipeListRow> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: 'No recipe-based products yet.',
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Product')),
        DataColumn(label: Text('Yield'), numeric: true),
        DataColumn(label: Text('Ingredients'), numeric: true),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final r in rows)
          DataRow(cells: [
            DataCell(Text(r.productCode, style: const TextStyle(color: Bo.textSubtle))),
            DataCell(Text(r.productName, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text('${r.yield}')),
            DataCell(Text('${r.itemCount}')),
            DataCell(r.itemCount == 0
                ? const ToneChip('No recipe', 'danger')
                : (r.isActive ? const ToneChip('Active', 'success') : const ToneChip('Inactive', 'neutral'))),
            DataCell(IconButton(
              tooltip: 'View recipe',
              icon: const Icon(Icons.receipt_long, size: 18),
              onPressed: r.itemCount == 0 ? null : () => _showRecipe(context, r),
            )),
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

  void _showRecipe(BuildContext context, RecipeListRow row) {
    showDialog<void>(
      context: context,
      builder: (_) => Dialog(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 560, maxHeight: 640),
          child: Consumer(builder: (context, ref, _) {
            final async = ref.watch(stockRecipeProvider(row.productId));
            return Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 14, 8, 14),
                  child: Row(children: [
                    const Icon(Icons.receipt_long, size: 20, color: Bo.textMuted),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text('${row.productName} · Recipe',
                          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: Bo.text)),
                    ),
                    IconButton(onPressed: () => Navigator.pop(context), icon: const Icon(Icons.close)),
                  ]),
                ),
                const Divider(height: 1),
                Flexible(
                  child: async.when(
                    loading: () => const Padding(
                        padding: EdgeInsets.all(40), child: Center(child: CircularProgressIndicator())),
                    error: (e, _) => Padding(
                        padding: const EdgeInsets.all(24),
                        child: Text('$e', style: const TextStyle(color: Bo.danger))),
                    data: (recipe) => recipe == null
                        ? const Padding(padding: EdgeInsets.all(24), child: Text('No recipe defined.'))
                        : ListView(
                            padding: const EdgeInsets.all(16),
                            children: [
                              Text('Yield: ${recipe.yield}',
                                  style: const TextStyle(color: Bo.textMuted)),
                              const SizedBox(height: 12),
                              for (final ri in recipe.items)
                                Padding(
                                  padding: const EdgeInsets.symmetric(vertical: 6),
                                  child: Row(children: [
                                    Expanded(child: Text('${ri.itemName} (${ri.itemCode})')),
                                    ToneChip('${ri.quantity} ${ri.unitCode}', 'info'),
                                  ]),
                                ),
                            ],
                          ),
                  ),
                ),
              ],
            );
          }),
        ),
      ),
    );
  }
}
