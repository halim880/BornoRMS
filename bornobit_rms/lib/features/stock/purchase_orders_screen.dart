import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'goods_receipt_form.dart';
import 'purchase_order_form.dart';
import 'stock_api.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const purchaseOrdersRoute = '/stock/po';

/// Stock → Purchase Orders. Full lifecycle: raise a draft, approve, then receive (GRN) or cancel.
/// Mirrors the Blazor PurchaseOrders.razor page.
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
          subtitle: 'Raise an order, approve it, then receive against it. Tap a row for line detail.',
          actions: [
            FilledButton.icon(
              onPressed: () => showPurchaseOrderForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New PO'),
            ),
            const SizedBox(width: 8),
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
            DataCell(Row(mainAxisSize: MainAxisSize.min, children: [
              IconButton(
                tooltip: 'View',
                icon: const Icon(Icons.open_in_new, size: 18),
                onPressed: () => _showDetail(context, ref, p.id),
              ),
              _RowMenu(po: p),
            ])),
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

/// Status-aware actions for a PO row. Draft → edit / approve / delete; Approved or
/// Partially-Received → cancel. Received / Cancelled are terminal (no menu).
class _RowMenu extends ConsumerWidget {
  final PurchaseOrderRow po;
  const _RowMenu({required this.po});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDraft = po.status == 1;
    final isOpen = po.status == 2 || po.status == 3; // Approved / PartiallyReceived
    if (!isDraft && !isOpen) return const SizedBox.shrink();

    return PopupMenuButton<String>(
      tooltip: 'Actions',
      icon: const Icon(Icons.more_vert, size: 18),
      onSelected: (v) => _onSelect(context, ref, v),
      itemBuilder: (_) => [
        if (isDraft) ...const [
          PopupMenuItem(value: 'edit', child: Text('Edit')),
          PopupMenuItem(value: 'approve', child: Text('Approve')),
          PopupMenuItem(value: 'delete', child: Text('Delete')),
        ],
        if (isOpen) ...const [
          PopupMenuItem(value: 'receive', child: Text('Receive (GRN)')),
          PopupMenuItem(value: 'cancel', child: Text('Cancel')),
        ],
      ],
    );
  }

  Future<void> _onSelect(BuildContext context, WidgetRef ref, String action) async {
    final api = ref.read(staffApiProvider);
    try {
      switch (action) {
        case 'edit':
          final detail = await ref.read(stockPurchaseOrderProvider(po.id).future);
          if (context.mounted) await showPurchaseOrderForm(context, existing: detail);
          return;
        case 'receive':
          final detail = await ref.read(stockPurchaseOrderProvider(po.id).future);
          if (context.mounted) await showGoodsReceiptForm(context, fromPo: detail);
          return;
        case 'approve':
          if (!await _confirm(context, 'Approve ${po.poNumber}?', 'Approve')) return;
          await api.stockApprovePurchaseOrder(po.id);
          if (context.mounted) _done(context, ref, 'Purchase order approved');
          return;
        case 'cancel':
          if (!await _confirm(context, 'Cancel ${po.poNumber}?', 'Cancel PO', danger: true)) return;
          await api.stockCancelPurchaseOrder(po.id);
          if (context.mounted) _done(context, ref, 'Purchase order cancelled');
          return;
        case 'delete':
          if (!await _confirm(context, 'Delete draft ${po.poNumber}?', 'Delete', danger: true)) return;
          await api.stockDeletePurchaseOrder(po.id);
          if (context.mounted) _done(context, ref, 'Purchase order deleted');
          return;
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _done(BuildContext context, WidgetRef ref, String msg) {
    ref.invalidate(stockPurchaseOrdersProvider);
    ref.invalidate(stockPurchaseOrderProvider(po.id));
    AppToast.show(context, msg);
  }

  Future<bool> _confirm(BuildContext context, String title, String confirmLabel, {bool danger = false}) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: Text(title),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Cancel')),
          FilledButton(
            style: danger ? FilledButton.styleFrom(backgroundColor: Bo.danger) : null,
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(confirmLabel),
          ),
        ],
      ),
    );
    return ok ?? false;
  }
}
