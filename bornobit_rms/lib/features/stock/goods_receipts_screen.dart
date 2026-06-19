import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const goodsReceiptsRoute = '/stock/grn';

/// Stock → Goods Receipts. Read-only list + detail dialog (the multi-line
/// create/post flow is deferred). Mirrors the Blazor GoodsReceipts.razor page.
class GoodsReceiptsScreen extends ConsumerWidget {
  const GoodsReceiptsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(stockGoodsReceiptsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Goods Receipts',
          subtitle: 'Stock received against purchase orders. Tap a row for detail.',
          actions: [
            IconButton(
              tooltip: 'Refresh',
              onPressed: () => ref.invalidate(stockGoodsReceiptsProvider),
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<Paged<GoodsReceiptRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockGoodsReceiptsProvider),
            data: (paged) => _table(context, ref, paged),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, WidgetRef ref, Paged<GoodsReceiptRow> paged) {
    return DataTableCard(
      emptyMessage: 'No goods receipts yet.',
      columns: const [
        DataColumn(label: Text('GRN #')),
        DataColumn(label: Text('Supplier')),
        DataColumn(label: Text('Invoice')),
        DataColumn(label: Text('Received')),
        DataColumn(label: Text('Lines'), numeric: true),
        DataColumn(label: Text('Subtotal'), numeric: true),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('')),
      ],
      rows: [
        for (final g in paged.items)
          DataRow(cells: [
            DataCell(Text(g.grnNumber, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(g.supplierName)),
            DataCell(Text(g.invoiceNo?.isNotEmpty == true ? g.invoiceNo! : '—',
                style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(shortDate(g.receivedAtUtc))),
            DataCell(Text('${g.lineCount}')),
            DataCell(Text(money(g.subtotal, g.currency))),
            DataCell(ToneChip(grnStatusLabel(g.status), grnStatusTone(g.status))),
            DataCell(IconButton(
              tooltip: 'View',
              icon: const Icon(Icons.open_in_new, size: 18),
              onPressed: () => _showDetail(context, ref, g.id),
            )),
          ]),
      ],
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} receipts',
        onPage: (pg) => ref.read(stockGrnPageProvider.notifier).state = pg,
      ),
    );
  }

  void _showDetail(BuildContext context, WidgetRef ref, String id) {
    showDialog<void>(
      context: context,
      builder: (_) => Dialog(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 720, maxHeight: 720),
          child: Consumer(builder: (context, ref, _) {
            final async = ref.watch(stockGoodsReceiptProvider(id));
            return async.when(
              loading: () => const SizedBox(
                  height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => Padding(
                  padding: const EdgeInsets.all(24),
                  child: Text('$e', style: const TextStyle(color: Bo.danger))),
              data: (grn) => _detail(context, grn),
            );
          }),
        ),
      ),
    );
  }

  Widget _detail(BuildContext context, GoodsReceiptDetail grn) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 8, 14),
          child: Row(children: [
            Expanded(
              child: Text('${grn.grnNumber} · ${grn.supplierName}',
                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: Bo.text)),
            ),
            ToneChip(grnStatusLabel(grn.status), grnStatusTone(grn.status)),
            const SizedBox(width: 4),
            IconButton(onPressed: () => Navigator.pop(context), icon: const Icon(Icons.close)),
          ]),
        ),
        const Divider(height: 1),
        Flexible(
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Wrap(spacing: 16, runSpacing: 8, children: [
                _meta('Received', shortDate(grn.receivedAtUtc)),
                _meta('Invoice', grn.invoiceNo?.isNotEmpty == true ? grn.invoiceNo! : '—'),
                _meta('Subtotal', money(grn.subtotal, grn.currency)),
              ]),
              if (grn.notes != null && grn.notes!.isNotEmpty) ...[
                const SizedBox(height: 12),
                Text(grn.notes!, style: const TextStyle(color: Bo.textMuted)),
              ],
              const SizedBox(height: 16),
              const Text('Lines', style: TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(height: 8),
              for (final l in grn.lines)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Row(children: [
                    Expanded(child: Text(l.itemName)),
                    Text('${l.qty} ${l.unitCode} · ${money(l.unitCost, grn.currency)}',
                        style: const TextStyle(color: Bo.textMuted)),
                    const SizedBox(width: 8),
                    Text(money(l.lineTotal, grn.currency),
                        style: const TextStyle(fontWeight: FontWeight.w700)),
                  ]),
                ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _meta(String label, String value) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: const TextStyle(fontSize: 12, color: Bo.textSubtle)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
        ],
      );
}
