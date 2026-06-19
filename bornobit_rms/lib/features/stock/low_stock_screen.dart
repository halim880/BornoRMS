import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const lowStockRoute = '/stock/low';

/// Stock → Low Stock. Active items at or below their reorder level.
/// Mirrors the Blazor LowStock.razor page.
class LowStockScreen extends ConsumerStatefulWidget {
  const LowStockScreen({super.key});

  @override
  ConsumerState<LowStockScreen> createState() => _LowStockScreenState();
}

class _LowStockScreenState extends ConsumerState<LowStockScreen> {
  static const _pageSize = 15;
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(stockLowStockProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Low Stock',
          subtitle: 'Items that need reordering — at or below reorder level.',
          actions: [
            IconButton(
              tooltip: 'Refresh',
              onPressed: () => ref.invalidate(stockLowStockProvider),
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<StockItem>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockLowStockProvider),
            data: (list) => _table(list),
          ),
        ),
      ],
    );
  }

  Widget _table(List<StockItem> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: 'Nothing below reorder level. Stock looks healthy.',
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Category')),
        DataColumn(label: Text('On Hand'), numeric: true),
        DataColumn(label: Text('Reorder Level'), numeric: true),
        DataColumn(label: Text('Reorder Qty'), numeric: true),
      ],
      rows: [
        for (final i in rows)
          DataRow(cells: [
            DataCell(Text(i.code, style: const TextStyle(color: Bo.textSubtle))),
            DataCell(Text(i.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(i.categoryName)),
            DataCell(Text('${i.qtyOnHand} ${i.unitCode}',
                style: const TextStyle(color: Bo.danger, fontWeight: FontWeight.w700))),
            DataCell(Text('${i.reorderLevel}')),
            DataCell(Text('${i.reorderQty}')),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} items',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }
}
