import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const skusRoute = '/stock/skus';

/// Stock → Product SKUs. Shows per-product/variant SKU coverage.
/// Mirrors the Blazor Skus.razor page.
class SkusScreen extends ConsumerStatefulWidget {
  const SkusScreen({super.key});

  @override
  ConsumerState<SkusScreen> createState() => _SkusScreenState();
}

class _SkusScreenState extends ConsumerState<SkusScreen> {
  static const _pageSize = 15;
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(stockSkusProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Product SKUs',
          subtitle: 'Stock items linked to each sellable product/variant slot.',
          actions: [
            IconButton(
              tooltip: 'Refresh',
              onPressed: () => ref.invalidate(stockSkusProvider),
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<ProductSkus>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockSkusProvider),
            data: (list) => _table(list),
          ),
        ),
      ],
    );
  }

  Widget _table(List<ProductSkus> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: 'No sellable products to map SKUs for.',
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Product')),
        DataColumn(label: Text('Coverage')),
        DataColumn(label: Text('Slots')),
      ],
      rows: [
        for (final p in rows)
          DataRow(cells: [
            DataCell(Text(p.code, style: const TextStyle(color: Bo.textSubtle))),
            DataCell(Text(p.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(p.covered == p.slots.length
                ? ToneChip('${p.covered}/${p.slots.length}', 'success')
                : ToneChip('${p.covered}/${p.slots.length}', p.covered == 0 ? 'danger' : 'warning')),
            DataCell(Wrap(
              spacing: 6,
              runSpacing: 4,
              children: [
                for (final s in p.slots)
                  ToneChip(
                    '${s.variantName ?? 'Base'}: ${s.hasSku ? '${s.qtyOnHand} ${s.unitCode}' : '—'}',
                    s.hasSku ? 'info' : 'neutral',
                  ),
              ],
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
}
