import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/widgets/async_view.dart';
import '../../core/widgets/format.dart';

Future<void> showSessionBill(BuildContext context, WidgetRef ref, String sessionId) {
  return showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.7,
      maxChildSize: 0.95,
      builder: (ctx, scroll) => _BillBody(sessionId: sessionId, scroll: scroll),
    ),
  );
}

class _BillBody extends ConsumerWidget {
  final String sessionId;
  final ScrollController scroll;
  const _BillBody({required this.sessionId, required this.scroll});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bill = ref.watch(sessionBillProvider(sessionId));
    return AsyncView(
      value: bill,
      onRetry: () => ref.invalidate(sessionBillProvider(sessionId)),
      data: (b) {
        return ListView(
          controller: scroll,
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
          children: [
            Text('Bill · Table ${b.tableNumber}',
                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text('${b.sessionNumber} · ${b.guestCount} guest(s)',
                style: TextStyle(color: Colors.grey.shade600, fontSize: 13)),
            const Divider(height: 24),
            for (final o in b.orders) ...[
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(o.orderNumber, style: const TextStyle(fontWeight: FontWeight.w700)),
                  Text('${o.status}${o.isPaid ? ' · paid' : ''}',
                      style: TextStyle(fontSize: 12, color: Colors.grey.shade600)),
                ],
              ),
              ...o.lines.map((l) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 2),
                    child: Row(
                      children: [
                        Expanded(child: Text('${l.quantity}× ${l.name}')),
                        Text(money(l.lineTotal, twoDp: true)),
                      ],
                    ),
                  )),
              const SizedBox(height: 10),
            ],
            const Divider(),
            _row('Subtotal', money(b.subtotal, currency: b.currency, twoDp: true)),
            if (b.discountAmount != 0)
              _row('Discount', '-${money(b.discountAmount, currency: b.currency, twoDp: true)}'),
            if (b.taxAmount != 0) _row('Tax', money(b.taxAmount, currency: b.currency, twoDp: true)),
            if (b.serviceChargeAmount != 0)
              _row('Service charge', money(b.serviceChargeAmount, currency: b.currency, twoDp: true)),
            _row('Grand total', money(b.grandTotal, currency: b.currency, twoDp: true), bold: true),
            if (b.paidAmount != 0)
              _row('Paid', money(b.paidAmount, currency: b.currency, twoDp: true)),
            if (b.balanceDue != 0)
              _row('Balance due', money(b.balanceDue, currency: b.currency, twoDp: true), bold: true),
          ],
        );
      },
    );
  }

  Widget _row(String label, String value, {bool bold = false}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 3),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label,
                style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
            Text(value, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
          ],
        ),
      );
}
