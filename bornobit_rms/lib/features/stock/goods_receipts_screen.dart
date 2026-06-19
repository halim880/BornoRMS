import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../accounts/accounts_providers.dart';
import '../dashboard/widgets.dart';
import 'goods_receipt_form.dart';
import 'stock_api.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const goodsReceiptsRoute = '/stock/grn';

/// Stock → Goods Receipts. Full flow: create a draft, then post it to raise stock + the supplier
/// payable (optionally paying at receipt). Mirrors the Blazor GoodsReceipts.razor page.
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
          subtitle: 'Receive stock, then post to raise inventory and the supplier payable.',
          actions: [
            FilledButton.icon(
              onPressed: () => showGoodsReceiptForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New GRN'),
            ),
            const SizedBox(width: 8),
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
            DataCell(Row(mainAxisSize: MainAxisSize.min, children: [
              IconButton(
                tooltip: 'View',
                icon: const Icon(Icons.open_in_new, size: 18),
                onPressed: () => _showDetail(context, ref, g.id),
              ),
              _GrnRowMenu(grn: g),
            ])),
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

/// Draft-GRN actions: edit, post (raise stock + payable, optionally pay now), or delete.
/// A Posted receipt is terminal here.
class _GrnRowMenu extends ConsumerWidget {
  final GoodsReceiptRow grn;
  const _GrnRowMenu({required this.grn});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (grn.status != 1) return const SizedBox.shrink(); // only Draft is actionable
    return PopupMenuButton<String>(
      tooltip: 'Actions',
      icon: const Icon(Icons.more_vert, size: 18),
      onSelected: (v) => _onSelect(context, ref, v),
      itemBuilder: (_) => const [
        PopupMenuItem(value: 'edit', child: Text('Edit')),
        PopupMenuItem(value: 'post', child: Text('Post (receive stock)')),
        PopupMenuItem(value: 'delete', child: Text('Delete')),
      ],
    );
  }

  Future<void> _onSelect(BuildContext context, WidgetRef ref, String action) async {
    final api = ref.read(staffApiProvider);
    try {
      switch (action) {
        case 'edit':
          final detail = await ref.read(stockGoodsReceiptProvider(grn.id).future);
          if (context.mounted) await showGoodsReceiptForm(context, existing: detail);
          return;
        case 'post':
          final choice = await _PostDialog.show(context, grn);
          if (choice == null) return; // cancelled
          await api.stockPostGoodsReceipt(grn.id, paymentCashAccountId: choice.cashAccountId);
          ref.invalidate(stockGoodsReceiptsProvider);
          ref.invalidate(stockGoodsReceiptProvider(grn.id));
          ref.invalidate(payablesProvider);
          if (context.mounted) AppToast.show(context, 'Goods receipt posted');
          return;
        case 'delete':
          final ok = await showDialog<bool>(
            context: context,
            builder: (_) => AlertDialog(
              title: Text('Delete draft ${grn.grnNumber}?'),
              actions: [
                TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Cancel')),
                FilledButton(
                  style: FilledButton.styleFrom(backgroundColor: Bo.danger),
                  onPressed: () => Navigator.of(context).pop(true),
                  child: const Text('Delete'),
                ),
              ],
            ),
          );
          if (ok != true) return;
          await api.stockDeleteGoodsReceipt(grn.id);
          ref.invalidate(stockGoodsReceiptsProvider);
          if (context.mounted) AppToast.show(context, 'Goods receipt deleted');
          return;
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }
}

/// Result of the post dialog: a cash account to pay from now, or null cashAccountId = post on credit.
class _PostChoice {
  final String? cashAccountId;
  const _PostChoice(this.cashAccountId);
}

/// Asks how to post: on credit (raises Accounts Payable) or paid now from a chosen cash account.
class _PostDialog extends ConsumerStatefulWidget {
  final GoodsReceiptRow grn;
  const _PostDialog({required this.grn});

  static Future<_PostChoice?> show(BuildContext context, GoodsReceiptRow grn) =>
      showDialog<_PostChoice>(context: context, builder: (_) => _PostDialog(grn: grn));

  @override
  ConsumerState<_PostDialog> createState() => _PostDialogState();
}

class _PostDialogState extends ConsumerState<_PostDialog> {
  bool _payNow = false;
  String? _cashAccountId;

  @override
  Widget build(BuildContext context) {
    final accounts = ref.watch(cashAccountsProvider);
    return AlertDialog(
      title: Text('Post ${widget.grn.grnNumber}'),
      content: SizedBox(
        width: 380,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Posting raises stock (${money(widget.grn.subtotal, widget.grn.currency)}) and the supplier payable.',
                style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
            const SizedBox(height: 12),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Pay supplier now'),
              subtitle: const Text('Off = post on credit (Accounts Payable)'),
              value: _payNow,
              onChanged: (v) => setState(() => _payNow = v),
            ),
            if (_payNow)
              accounts.when(
                loading: () => const LinearProgressIndicator(),
                error: (e, _) => Text('Cash accounts: $e', style: const TextStyle(color: Bo.danger)),
                data: (list) => DropdownButtonFormField<String>(
                  initialValue: _cashAccountId,
                  decoration: const InputDecoration(labelText: 'Pay from'),
                  items: [for (final c in list) DropdownMenuItem(value: c.id, child: Text(c.name))],
                  onChanged: (v) => setState(() => _cashAccountId = v),
                ),
              ),
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Cancel')),
        FilledButton(
          onPressed: (_payNow && _cashAccountId == null)
              ? null
              : () => Navigator.of(context).pop(_PostChoice(_payNow ? _cashAccountId : null)),
          child: const Text('Post'),
        ),
      ],
    );
  }
}
