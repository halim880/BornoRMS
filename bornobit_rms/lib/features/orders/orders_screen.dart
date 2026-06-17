import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../dashboard/widgets.dart';

const _statuses = ['Placed', 'Confirmed', 'Preparing', 'Ready', 'Served', 'Completed', 'Cancelled'];

class OrdersScreen extends ConsumerWidget {
  const OrdersScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final status = ref.watch(ordersStatusProvider);
    final async = ref.watch(ordersProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Filters
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _FilterChip(
                label: 'All',
                selected: status == null,
                onTap: () {
                  ref.read(ordersStatusProvider.notifier).state = null;
                  ref.read(ordersPageProvider.notifier).state = 1;
                },
              ),
              for (final s in _statuses)
                _FilterChip(
                  label: s,
                  selected: status == s,
                  onTap: () {
                    ref.read(ordersStatusProvider.notifier).state = s;
                    ref.read(ordersPageProvider.notifier).state = 1;
                  },
                ),
            ],
          ),
        ),
        Expanded(
          child: async.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => Center(
              child: Column(mainAxisSize: MainAxisSize.min, children: [
                const Icon(Icons.cloud_off, color: Bo.textSubtle, size: 36),
                const SizedBox(height: 8),
                Text(e.toString(), style: const TextStyle(color: Bo.textMuted)),
                const SizedBox(height: 12),
                FilledButton.icon(
                  onPressed: () => ref.invalidate(ordersProvider),
                  icon: const Icon(Icons.refresh),
                  label: const Text('Retry'),
                ),
              ]),
            ),
            data: (paged) => _OrdersTable(paged: paged),
          ),
        ),
      ],
    );
  }
}

class _OrdersTable extends ConsumerWidget {
  final Paged<OrderListItem> paged;
  const _OrdersTable({required this.paged});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (paged.items.isEmpty) {
      return const Center(child: Text('No orders match this filter', style: TextStyle(color: Bo.textSubtle)));
    }
    return Column(
      children: [
        Expanded(
          child: SingleChildScrollView(
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: DataTable(
                  columnSpacing: 28,
                  showCheckboxColumn: false,
                  columns: const [
                    DataColumn(label: Text('Order')),
                    DataColumn(label: Text('Customer')),
                    DataColumn(label: Text('Table')),
                    DataColumn(label: Text('Type')),
                    DataColumn(label: Text('Time')),
                    DataColumn(label: Text('Items')),
                    DataColumn(label: Text('Total')),
                    DataColumn(label: Text('Paid')),
                    DataColumn(label: Text('Status')),
                  ],
                  rows: [
                    for (final o in paged.items)
                      DataRow(
                        onSelectChanged: (_) => _openDetail(context, o.id),
                        cells: [
                          DataCell(Text(o.orderNumber, style: const TextStyle(fontWeight: FontWeight.w600))),
                          DataCell(Text(o.customerName?.isNotEmpty == true ? o.customerName! : o.customerPhone)),
                          DataCell(Text(o.tableNumber ?? '—')),
                          DataCell(Text(o.orderType)),
                          DataCell(Text(shortDateTime(o.orderedAtUtc))),
                          DataCell(Text(count(o.itemCount))),
                          DataCell(Text(money(o.total, o.currency))),
                          DataCell(o.isPaid
                              ? const ToneChip('Paid', 'success')
                              : const ToneChip('Unpaid', 'warning')),
                          DataCell(ToneChip(o.status, orderStatusTone(o.status))),
                        ],
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
        const Divider(height: 1),
        _Pager(paged: paged),
      ],
    );
  }

  void _openDetail(BuildContext context, String id) {
    showDialog(context: context, builder: (_) => _OrderDetailDialog(orderId: id));
  }
}

class _Pager extends ConsumerWidget {
  final Paged<OrderListItem> paged;
  const _Pager({required this.paged});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final page = paged.page;
    final totalPages = paged.totalPages;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          Text('${paged.totalCount} orders', style: const TextStyle(color: Bo.textSubtle, fontSize: 13)),
          const Spacer(),
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed: page > 1 ? () => ref.read(ordersPageProvider.notifier).state = page - 1 : null,
          ),
          Text('Page $page of ${totalPages == 0 ? 1 : totalPages}', style: const TextStyle(fontSize: 13)),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: page < totalPages ? () => ref.read(ordersPageProvider.notifier).state = page + 1 : null,
          ),
        ],
      ),
    );
  }
}

class _FilterChip extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;
  const _FilterChip({required this.label, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return ChoiceChip(
      label: Text(label),
      selected: selected,
      onSelected: (_) => onTap(),
      showCheckmark: false,
      selectedColor: Bo.primary,
      labelStyle: TextStyle(color: selected ? Colors.white : Bo.textMuted, fontSize: 13),
      backgroundColor: Bo.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(Bo.radiusSm),
        side: const BorderSide(color: Bo.border),
      ),
    );
  }
}

class _OrderDetailDialog extends ConsumerWidget {
  final String orderId;
  const _OrderDetailDialog({required this.orderId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(orderDetailProvider(orderId));
    return Dialog(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 560, maxHeight: 640),
        child: async.when(
          loading: () => const SizedBox(height: 240, child: Center(child: CircularProgressIndicator())),
          error: (e, _) => SizedBox(
            height: 240,
            child: Center(child: Text(e.toString(), style: const TextStyle(color: Bo.textMuted))),
          ),
          data: (o) => _DetailBody(o: o),
        ),
      ),
    );
  }
}

class _DetailBody extends StatelessWidget {
  final OrderDetail o;
  const _DetailBody({required this.o});

  @override
  Widget build(BuildContext context) {
    Widget totalRow(String label, double v, {bool bold = false}) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 2),
          child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
            Text(label, style: TextStyle(fontSize: 13, color: bold ? Bo.text : Bo.textMuted, fontWeight: bold ? FontWeight.w700 : FontWeight.w400)),
            Text(money(v, o.currency), style: TextStyle(fontSize: 13, fontWeight: bold ? FontWeight.w800 : FontWeight.w600)),
          ]),
        );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Header
        Container(
          padding: const EdgeInsets.all(16),
          decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: Bo.border))),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(o.orderNumber, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                    const SizedBox(height: 2),
                    Text('${o.orderType} · ${shortDateTime(o.orderedAtUtc)}',
                        style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                  ],
                ),
              ),
              ToneChip(o.status, orderStatusTone(o.status)),
              const SizedBox(width: 8),
              IconButton(onPressed: () => Navigator.of(context).pop(), icon: const Icon(Icons.close)),
            ],
          ),
        ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _kv('Customer', o.customerName?.isNotEmpty == true ? '${o.customerName} (${o.customerPhone})' : o.customerPhone),
              if (o.tableNumber != null) _kv('Table', o.tableNumber!),
              if (o.waiterName != null) _kv('Waiter', o.waiterName!),
              if (o.notes != null && o.notes!.isNotEmpty) _kv('Notes', o.notes!),
              const SizedBox(height: 12),
              const Text('Items', style: TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(height: 6),
              for (final l in o.lines)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 3),
                  child: Row(
                    children: [
                      Text('${l.quantity}×', style: const TextStyle(fontWeight: FontWeight.w700, color: Bo.textMuted)),
                      const SizedBox(width: 8),
                      Expanded(child: Text(l.name, overflow: TextOverflow.ellipsis)),
                      Text(money(l.lineTotal, o.currency), style: const TextStyle(fontWeight: FontWeight.w600)),
                    ],
                  ),
                ),
              const Divider(),
              totalRow('Subtotal', o.subtotal),
              if (o.discountAmount != 0) totalRow('Discount', -o.discountAmount),
              if (o.taxAmount != 0) totalRow('Tax', o.taxAmount),
              if (o.serviceChargeAmount != 0) totalRow('Service charge', o.serviceChargeAmount),
              if (o.tipAmount != 0) totalRow('Tip', o.tipAmount),
              totalRow('Grand total', o.grandTotal, bold: true),
              const SizedBox(height: 8),
              Row(children: [
                o.isPaid ? const ToneChip('Paid', 'success') : const ToneChip('Unpaid', 'warning'),
                const SizedBox(width: 8),
                if (o.paymentMethod != null) Text(o.paymentMethod!, style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                const Spacer(),
                if (o.balanceDue > 0) Text('Balance due ${money(o.balanceDue, o.currency)}', style: const TextStyle(color: Bo.warning, fontWeight: FontWeight.w600, fontSize: 12)),
              ]),
            ],
          ),
        ),
      ],
    );
  }

  Widget _kv(String k, String v) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 3),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          SizedBox(width: 90, child: Text(k, style: const TextStyle(color: Bo.textSubtle, fontSize: 13))),
          Expanded(child: Text(v, style: const TextStyle(fontSize: 13))),
        ]),
      );
}
