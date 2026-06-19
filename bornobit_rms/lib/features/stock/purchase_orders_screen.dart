import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const purchaseOrdersRoute = '/stock/po';

/// Stock → Purchase Orders. Read-only list + detail dialog (the multi-line
/// create/receive flow is deferred). Mirrors the Blazor PurchaseOrders.razor page.
class PurchaseOrdersScreen extends ConsumerWidget {
  const PurchaseOrdersScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(stockPurchaseOrdersProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Purchase Orders',
          subtitle: 'Orders raised against suppliers. Tap a row for line detail.',
          actions: [
            IconButton(
              tooltip: 'Refresh',
              onPressed: () => ref.invalidate(stockPurchaseOrdersProvider),
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<Paged<PurchaseOrderRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockPurchaseOrdersProvider),
            data: (paged) => _table(context, ref, paged),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, WidgetRef ref, Paged<PurchaseOrderRow> paged) {
    return DataTableCard(
      emptyMessage: 'No purchase orders yet.',
      columns: const [
        DataColumn(label: Text('PO #')),
        DataColumn(label: Text('Supplier')),
        DataColumn(label: Text('Ordered')),
        DataColumn(label: Text('Expected')),
        DataColumn(label: Text('Lines'), numeric: true),
        DataColumn(label: Text('Subtotal'), numeric: true),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('')),
      ],
      rows: [
        for (final p in paged.items)
          DataRow(cells: [
            DataCell(Text(p.poNumber, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(p.supplierName)),
            DataCell(Text(shortDate(p.orderedAtUtc))),
            DataCell(Text(p.expectedAtUtc == null ? '—' : shortDate(p.expectedAtUtc!))),
            DataCell(Text('${p.lineCount}')),
            DataCell(Text(money(p.subtotal, p.currency))),
            DataCell(ToneChip(poStatusLabel(p.status), poStatusTone(p.status))),
            DataCell(IconButton(
              tooltip: 'View',
              icon: const Icon(Icons.open_in_new, size: 18),
              onPressed: () => _showDetail(context, ref, p.id),
            )),
          ]),
      ],
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} orders',
        onPage: (pg) => ref.read(stockPoPageProvider.notifier).state = pg,
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
            final async = ref.watch(stockPurchaseOrderProvider(id));
            return async.when(
              loading: () => const SizedBox(
                  height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => Padding(
                  padding: const EdgeInsets.all(24),
                  child: Text('$e', style: const TextStyle(color: Bo.danger))),
              data: (po) => _detail(context, po),
            );
          }),
        ),
      ),
    );
  }

  Widget _detail(BuildContext context, PurchaseOrderDetail po) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 8, 14),
          child: Row(children: [
            Expanded(
              child: Text('${po.poNumber} · ${po.supplierName}',
                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: Bo.text)),
            ),
            ToneChip(poStatusLabel(po.status), poStatusTone(po.status)),
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
                _meta('Ordered', shortDate(po.orderedAtUtc)),
                _meta('Expected', po.expectedAtUtc == null ? '—' : shortDate(po.expectedAtUtc!)),
                _meta('Subtotal', money(po.subtotal, po.currency)),
              ]),
              if (po.notes != null && po.notes!.isNotEmpty) ...[
                const SizedBox(height: 12),
                Text(po.notes!, style: const TextStyle(color: Bo.textMuted)),
              ],
              const SizedBox(height: 16),
              const Text('Lines', style: TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(height: 8),
              for (final l in po.lines)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Row(children: [
                    Expanded(child: Text(l.itemName)),
                    Text('${l.qtyOrdered} ${l.unitCode} · ${money(l.unitCost, po.currency)}',
                        style: const TextStyle(color: Bo.textMuted)),
                    const SizedBox(width: 8),
                    Text(money(l.lineTotal, po.currency),
                        style: const TextStyle(fontWeight: FontWeight.w700)),
                  ]),
                ),
              if (po.receipts.isNotEmpty) ...[
                const SizedBox(height: 16),
                const Text('Goods Receipts', style: TextStyle(fontWeight: FontWeight.w700)),
                const SizedBox(height: 8),
                for (final r in po.receipts)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(children: [
                      Expanded(child: Text('${r.grnNumber} · ${shortDate(r.receivedAtUtc)}')),
                      ToneChip(grnStatusLabel(r.status), grnStatusTone(r.status)),
                      const SizedBox(width: 8),
                      Text(money(r.subtotal, po.currency)),
                    ]),
                  ),
              ],
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
