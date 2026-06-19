import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storeCategoriesRoute = '/store/categories';

/// Store → Categories. Mirrors StoreCategories.razor.
class StoreCategoriesScreen extends ConsumerWidget {
  const StoreCategoriesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeCategoriesProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Store Categories',
          subtitle: 'Grouping for stock items.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeCategoriesProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<StoreCategory>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeCategoriesProvider),
            data: _body,
          ),
        ),
      ],
    );
  }

  Widget _body(List<StoreCategory> rows) {
    return DataTableCard(
      emptyMessage: 'No categories yet.',
      columns: const [
        DataColumn(label: Text('Order'), numeric: true),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Bangla')),
        DataColumn(label: Text('Description')),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final c in rows)
          DataRow(cells: [
            DataCell(Text(c.displayOrder.toString())),
            DataCell(Text(c.name, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(Text(c.banglaName ?? '—')),
            DataCell(Text(c.description ?? '—')),
            DataCell(ToneChip(c.isActive ? 'Active' : 'Inactive', c.isActive ? 'success' : 'neutral')),
          ]),
      ],
    );
  }
}
