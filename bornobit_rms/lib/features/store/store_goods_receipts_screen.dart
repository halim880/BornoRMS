import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storeGoodsReceiptsRoute = '/store/grn';

/// Store → Goods Receipts (GRN). Paginated. Mirrors StoreGoodsReceipts.razor.
class StoreGoodsReceiptsScreen extends ConsumerWidget {
  const StoreGoodsReceiptsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storeGoodsReceiptsProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Goods Receipts',
          subtitle: 'Stock received from suppliers (GRN).',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storeGoodsReceiptsProvider))],
        ),
        Expanded(
          child: AsyncStateView<Paged<StoreGoodsReceipt>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storeGoodsReceiptsProvider),
            data: (paged) => _body(ref, paged),
          ),
        ),
      ],
    );
  }

  Widget _body(WidgetRef ref, Paged<StoreGoodsReceipt> paged) {
    return DataTableCard(
      emptyMessage: 'No goods receipts yet.',
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} receipts',
        onPage: (p) => ref.read(storeGrnPageProvider.notifier).state = p,
      ),
      columns: const [
        DataColumn(label: Text('GRN #')),
        DataColumn(label: Text('Supplier')),
        DataColumn(label: Text('Invoice #')),
        DataColumn(label: Text('Received')),
        DataColumn(label: Text('Lines'), numeric: true),
        DataColumn(label: Text('Subtotal'), numeric: true),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final g in paged.items)
          DataRow(cells: [
            DataCell(Text(g.grnNumber, style: const TextStyle(fontWeight: FontWeight.w600))),
            DataCell(Text(g.supplierName)),
            DataCell(Text(g.invoiceNo ?? '—')),
            DataCell(Text(shortDate(g.receivedAtUtc))),
            DataCell(Text(g.lineCount.toString())),
            DataCell(Text(money(g.subtotal, g.currency))),
            DataCell(ToneChip(g.status, storeDocStatusTone(g.status))),
          ]),
      ],
    );
  }
}

/// Tone for GRN / Issue lifecycle (Draft / Posted / Voided).
String storeDocStatusTone(String s) => switch (s) {
      'Draft' => 'neutral',
      'Posted' => 'success',
      'Voided' => 'danger',
      _ => 'neutral',
    };
