import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storeRequisitionsRoute = '/store/requisitions';

/// Store → Requisitions. Paginated. Mirrors StoreRequisitions.razor.
class StoreRequisitionsScreen extends ConsumerWidget {
  const StoreRequisitionsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeRequisitionsProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Requisitions',
          subtitle: 'Department requests for stock.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeRequisitionsProvider))],
        ),
        Expanded(
          child: AsyncStateView<Paged<StoreRequisition>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeRequisitionsProvider),
            data: (paged) => _body(ref, paged),
          ),
        ),
      ],
    );
  }

  Widget _body(WidgetRef ref, Paged<StoreRequisition> paged) {
    return DataTableCard(
      emptyMessage: 'No requisitions yet.',
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} requisitions',
        onPage: (p) => ref.read(storeRequisitionsPageProvider.notifier).state = p,
      ),
      columns: const [
        DataColumn(label: Text('Req #')),
        DataColumn(label: Text('Department')),
        DataColumn(label: Text('Requested')),
        DataColumn(label: Text('Required By')),
        DataColumn(label: Text('Lines'), numeric: true),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final r in paged.items)
          DataRow(cells: [
            DataCell(Text(r.requisitionNumber, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(Text(r.departmentName)),
            DataCell(Text(shortDate(r.requestedAtUtc))),
            DataCell(Text(r.requiredByUtc == null ? '—' : shortDate(r.requiredByUtc!))),
            DataCell(Text(r.lineCount.toString())),
            DataCell(ToneChip(r.status, _reqStatusTone(r.status))),
          ]),
      ],
    );
  }

  String _reqStatusTone(String s) => switch (s) {
        'Draft' => 'neutral',
        'Submitted' => 'info',
        'Approved' => 'primary',
        'Rejected' => 'danger',
        'PartiallyIssued' => 'warning',
        'Issued' => 'success',
        'Cancelled' => 'danger',
        _ => 'neutral',
      };
}
