import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storeDepartmentsRoute = '/store/departments';

/// Store → Departments. Consuming departments for requisitions/issues.
/// Mirrors StoreDepartments.razor.
class StoreDepartmentsScreen extends ConsumerWidget {
  const StoreDepartmentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeDepartmentsProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Store Departments',
          subtitle: 'Departments that requisition and consume stock.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeDepartmentsProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<StoreDepartment>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeDepartmentsProvider),
            data: _body,
          ),
        ),
      ],
    );
  }

  Widget _body(List<StoreDepartment> rows) {
    return DataTableCard(
      emptyMessage: 'No departments yet.',
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Bangla')),
        DataColumn(label: Text('Order'), numeric: true),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final d in rows)
          DataRow(cells: [
            DataCell(Text(d.code)),
            DataCell(Text(d.name, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(Text(d.banglaName ?? '—')),
            DataCell(Text(d.displayOrder.toString())),
            DataCell(ToneChip(d.isActive ? 'Active' : 'Inactive', d.isActive ? 'success' : 'neutral')),
          ]),
      ],
    );
  }
}
