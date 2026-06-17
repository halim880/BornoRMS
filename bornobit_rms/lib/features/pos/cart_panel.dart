import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/printing/print_service.dart';
import '../../core/theme/app_colors.dart';
import '../dashboard/widgets.dart' show money;
import 'cart_line.dart';
import 'payment_dialog.dart';
import 'pos_dialogs.dart';
import 'pos_providers.dart';
import 'pos_section.dart';
import 'summary_bar.dart';

class CartPanel extends ConsumerWidget {
  const CartPanel({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final a = context.appColors;
    final state = ref.watch(posControllerProvider);
    final detail = state.detail;

    if (detail == null) {
      return PosPanel(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text(
              'Pick an order above, or tap + to start one.',
              textAlign: TextAlign.center,
              style: TextStyle(color: a.textTertiary),
            ),
          ),
        ),
      );
    }

    final c = ref.read(posControllerProvider.notifier);
    final busy = state.busy;

    return PosPanel(
      child: Column(
        children: [
          _Header(detail: detail),
          const Divider(height: 1),
          // lines
          Expanded(
            child: detail.lines.isEmpty
                ? _EmptyItems()
                : ListView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 14),
                    itemCount: detail.lines.length,
                    itemBuilder: (_, i) => CartLineRow(
                      line: detail.lines[i],
                      currency: detail.currency,
                      onQty: (d) => c.changeQty(detail.lines[i], d),
                      onRemove: () => c.removeLine(detail.lines[i]),
                    ),
                  ),
          ),
          Divider(height: 1, color: a.border),
          // summary + actions
          Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              children: [
                SummaryBar(detail: detail),
                const SizedBox(height: 14),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: busy ? null : () => _print(context, ref, detail, kot: false),
                        icon: const Icon(Icons.receipt_long, size: 18),
                        label: const Text('Receipt'),
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: (busy || detail.lines.isEmpty)
                            ? null
                            : () => _print(context, ref, detail, kot: true),
                        icon: const Icon(Icons.soup_kitchen_outlined, size: 18),
                        label: const Text('Send to kitchen'),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    TextButton(
                      onPressed: busy ? null : () => showCancelDialog(context, ref),
                      child: const Text('Cancel'),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: FilledButton(
                        onPressed: (busy || detail.lines.isEmpty)
                            ? null
                            : () => _checkout(context, ref),
                        style: FilledButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 14)),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            const Text('Charge'),
                            Text(
                              money(detail.grandTotal, detail.currency),
                              style: AppColors.priceText.copyWith(color: a.onAccent),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _checkout(BuildContext context, WidgetRef ref) async {
    final result = await showDialog(context: context, builder: (_) => const PaymentDialog());
    if (result != null) {
      final detail = ref.read(posControllerProvider).detail;
      if (detail != null && context.mounted) {
        await _print(context, ref, detail, kot: false);
      }
      ref.read(posControllerProvider.notifier).clearSelection();
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Order settled.')));
      }
    }
  }

  Future<void> _print(BuildContext context, WidgetRef ref, OrderDetail detail,
      {required bool kot}) async {
    final svc = ref.read(printServiceProvider);
    final outcome = kot ? await svc.printKot(detail) : await svc.printReceipt(detail);
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(outcome.message)));
    }
  }
}

class _Header extends StatelessWidget {
  final OrderDetail detail;
  const _Header({required this.detail});

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    final walkIn = detail.customerName == null || detail.customerName!.trim().isEmpty;
    return Padding(
      padding: const EdgeInsets.fromLTRB(14, 12, 8, 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(detail.orderNumber,
                    style: Theme.of(context).textTheme.titleMedium),
              ),
              IconButton(
                visualDensity: VisualDensity.compact,
                icon: Icon(Icons.edit_outlined, size: 18, color: a.textSecondary),
                onPressed: () => showDialog(
                    context: context, builder: (_) => const NewOrderDialog(edit: true)),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              _Chip(
                label: walkIn ? 'Walk-in' : detail.customerName!,
                bg: walkIn ? a.successTint : a.surfaceMuted,
                border: walkIn ? a.successBorder : a.border,
                fg: walkIn ? a.success : a.textSecondary,
                dot: walkIn ? a.success : null,
              ),
              const SizedBox(width: 8),
              _Chip(
                label: detail.orderType,
                bg: a.surfaceMuted,
                border: a.border,
                fg: a.textSecondary,
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Chip extends StatelessWidget {
  final String label;
  final Color bg;
  final Color border;
  final Color fg;
  final Color? dot;
  const _Chip({
    required this.label,
    required this.bg,
    required this.border,
    required this.fg,
    this.dot,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(AppColors.radiusChip),
        border: Border.all(color: border),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (dot != null) ...[
            Container(width: 7, height: 7, decoration: BoxDecoration(color: dot, shape: BoxShape.circle)),
            const SizedBox(width: 6),
          ],
          Text(label,
              style: TextStyle(color: fg, fontSize: 13, fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }
}

class _EmptyItems extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    final text = Theme.of(context).textTheme;
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.shopping_cart_outlined, size: 40, color: a.textTertiary),
          const SizedBox(height: 10),
          Text('No items yet', style: text.bodyLarge),
          const SizedBox(height: 4),
          Text('Tap a product to start this order.', style: text.bodySmall),
        ],
      ),
    );
  }
}
