import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storeSuppliersRoute = '/store/suppliers';

/// Store → Suppliers. Mirrors StoreSuppliers.razor.
class StoreSuppliersScreen extends ConsumerWidget {
  const StoreSuppliersScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeSuppliersProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Store Suppliers',
          subtitle: 'Vendors that supply stock items.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeSuppliersProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<StoreSupplier>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeSuppliersProvider),
            data: _body,
          ),
        ),
      ],
    );
  }

  Widget _body(List<StoreSupplier> rows) {
    return DataTableCard(
      emptyMessage: 'No suppliers yet.',
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Phone')),
        DataColumn(label: Text('Terms (days)'), numeric: true),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final s in rows)
          DataRow(cells: [
            DataCell(Text(s.code)),
            DataCell(Text(s.name, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(Text(s.phone ?? '—')),
            DataCell(Text(s.paymentTermsDays.toString())),
            DataCell(ToneChip(s.isActive ? 'Active' : 'Inactive', s.isActive ? 'success' : 'neutral')),
          ]),
      ],
    );
  }
}
