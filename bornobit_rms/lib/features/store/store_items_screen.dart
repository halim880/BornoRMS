import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storeItemsRoute = '/store/items';

/// Store → Items. Paginated stock-item list. Mirrors StoreItems.razor.
class StoreItemsScreen extends ConsumerWidget {
  const StoreItemsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeItemsProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Store Items',
          subtitle: 'Stock items with on-hand quantity and value.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeItemsProvider))],
        ),
        Expanded(
          child: AsyncStateView<Paged<StoreItem>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeItemsProvider),
            data: (paged) => _body(ref, paged),
          ),
        ),
      ],
    );
  }

  Widget _body(WidgetRef ref, Paged<StoreItem> paged) {
    return DataTableCard(
      emptyMessage: 'No store items yet.',
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} items',
        onPage: (p) => ref.read(storeItemsPageProvider.notifier).state = p,
      ),
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Category')),
        DataColumn(label: Text('Unit')),
        DataColumn(label: Text('On Hand'), numeric: true),
        DataColumn(label: Text('Avg Cost'), numeric: true),
        DataColumn(label: Text('Value'), numeric: true),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final i in paged.items)
          DataRow(cells: [
            DataCell(Text(i.code)),
            DataCell(Text(i.name, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(Text(i.categoryName)),
            DataCell(Text(i.unitCode)),
            DataCell(Text(i.qtyOnHand.toString(),
                style: TextStyle(
                    color: i.isLowStock ? Bo.danger : Bo.text,
                    fontWeight: i.isLowStock ? FontWeight.w700 : FontWeight.w400))),
            DataCell(Text(money(i.avgCost, i.currency))),
            DataCell(Text(money(i.stockValue, i.currency))),
            DataCell(Wrap(spacing: 4, children: [
              if (i.isLowStock) const ToneChip('Low', 'danger'),
              ToneChip(i.isActive ? 'Active' : 'Inactive', i.isActive ? 'success' : 'neutral'),
            ])),
          ]),
      ],
    );
  }
}
