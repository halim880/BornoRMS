import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_goods_receipts_screen.dart' show storeDocStatusTone;
import 'store_models.dart';
import 'store_providers.dart';

const storeIssuesRoute = '/store/issues';

/// Store → Issues. Paginated stock issues to departments. Mirrors StoreIssues.razor.
class StoreIssuesScreen extends ConsumerWidget {
  const StoreIssuesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeIssuesProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Store Issues',
          subtitle: 'Stock issued out to departments.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeIssuesProvider))],
        ),
        Expanded(
          child: AsyncStateView<Paged<StoreIssue>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeIssuesProvider),
            data: (paged) => _body(ref, paged),
          ),
        ),
      ],
    );
  }

  Widget _body(WidgetRef ref, Paged<StoreIssue> paged) {
    return DataTableCard(
      emptyMessage: 'No issues yet.',
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} issues',
        onPage: (p) => ref.read(storeIssuesPageProvider.notifier).state = p,
      ),
      columns: const [
        DataColumn(label: Text('Issue #')),
        DataColumn(label: Text('Destination')),
        DataColumn(label: Text('Requisition')),
        DataColumn(label: Text('Issued')),
        DataColumn(label: Text('Lines'), numeric: true),
        DataColumn(label: Text('Total Qty'), numeric: true),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final i in paged.items)
          DataRow(cells: [
            DataCell(Text(i.issueNumber, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(Text(i.destination)),
            DataCell(Text(i.requisitionNumber ?? '—')),
            DataCell(Text(shortDate(i.issuedAtUtc))),
            DataCell(Text(i.lineCount.toString())),
            DataCell(Text(i.totalQtyBase.toString())),
            DataCell(ToneChip(i.status, storeDocStatusTone(i.status))),
          ]),
      ],
    );
  }
}
