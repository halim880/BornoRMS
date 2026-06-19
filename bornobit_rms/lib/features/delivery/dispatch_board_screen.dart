import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'delivery_api.dart';
import 'delivery_models.dart';
import 'delivery_providers.dart';

const dispatchBoardRoute = '/logistics/dispatch';

/// Delivery → Dispatch Board. Live board of today's delivery orders grouped by
/// dispatch stage, with assign-rider and status actions. COD is settled by the
/// cashier on the POS when the rider returns — the board shows COD-expected.
class DispatchBoardScreen extends ConsumerWidget {
  const DispatchBoardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(deliveryBoardProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Dispatch Board',
          subtitle: 'Assign riders and track delivery orders. COD is collected at the counter on return.',
          actions: [
            IconButton(
              tooltip: 'Refresh',
              icon: const Icon(Icons.refresh, color: Bo.textMuted),
              onPressed: () => ref.read(deliveryBoardProvider.notifier).refresh(),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<Paged<DeliveryBoardRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.read(deliveryBoardProvider.notifier).refresh(),
            data: (paged) => _board(context, ref, paged.items),
          ),
        ),
      ],
    );
  }

  Widget _board(BuildContext context, WidgetRef ref, List<DeliveryBoardRow> rows) {
    if (rows.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(40),
          child: Text('No delivery orders today.', style: TextStyle(color: Bo.textMuted)),
        ),
      );
    }

    const sections = <(String, List<DeliveryStatus>)>[
      ('Pending', [DeliveryStatus.pending]),
      ('Assigned', [DeliveryStatus.assigned]),
      ('Out for delivery', [DeliveryStatus.outForDelivery]),
      ('Delivered', [DeliveryStatus.delivered]),
      ('Failed', [DeliveryStatus.failed]),
    ];

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      children: [
        for (final (title, statuses) in sections)
          ...() {
            final group = rows.where((r) => statuses.contains(r.deliveryStatus)).toList();
            if (group.isEmpty) return <Widget>[];
            return [
              Padding(
                padding: const EdgeInsets.fromLTRB(4, 12, 4, 6),
                child: Text('$title · ${group.length}',
                    style: const TextStyle(fontWeight: FontWeight.w800, color: Bo.textSubtle)),
              ),
              for (final r in group) _card(context, ref, r),
            ];
          }(),
      ],
    );
  }

  Widget _card(BuildContext context, WidgetRef ref, DeliveryBoardRow r) {
    return Container(
      margin: const EdgeInsets.symmetric(vertical: 4),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: Bo.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(r.orderNumber, style: const TextStyle(fontWeight: FontWeight.w800)),
              const SizedBox(width: 8),
              if (r.isCod)
                ToneChip('COD ${money(r.codExpected, 'Tk')}', 'warning')
              else if (r.isPaid)
                const ToneChip('Paid', 'success'),
              const Spacer(),
              if (r.riderName != null)
                Text(r.riderName!, style: const TextStyle(fontSize: 12, color: Bo.textSubtle)),
            ],
          ),
          const SizedBox(height: 6),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(Icons.place_outlined, size: 16, color: Bo.textMuted),
              const SizedBox(width: 4),
              Expanded(child: Text(r.address, style: const TextStyle(fontSize: 13))),
              if (r.contactPhone != null) ...[
                const Icon(Icons.phone_outlined, size: 14, color: Bo.textMuted),
                const SizedBox(width: 3),
                Text(r.contactPhone!, style: const TextStyle(fontSize: 12, color: Bo.textMuted)),
              ],
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Text(money(r.grandTotal, 'Tk'),
                  style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
              const Spacer(),
              ..._actions(context, ref, r),
            ],
          ),
        ],
      ),
    );
  }

  List<Widget> _actions(BuildContext context, WidgetRef ref, DeliveryBoardRow r) {
    switch (r.deliveryStatus) {
      case DeliveryStatus.pending:
        return [
          _btn('Assign rider', Icons.person_add_alt, () => _assign(context, ref, r)),
          _iconBtn('Cancel', Icons.close, Bo.danger, () => _cancel(context, ref, r)),
        ];
      case DeliveryStatus.assigned:
        return [
          _btn('Out for delivery', Icons.local_shipping_outlined, () => _act(context, ref, () => ref.read(staffApiProvider).markOutForDelivery(r.orderId), 'Dispatched')),
          _iconBtn('Reassign', Icons.swap_horiz, Bo.textMuted, () => _assign(context, ref, r)),
          _iconBtn('Cancel', Icons.close, Bo.danger, () => _cancel(context, ref, r)),
        ];
      case DeliveryStatus.outForDelivery:
        return [
          _btn('Delivered', Icons.check_circle_outline, () => _act(context, ref, () => ref.read(staffApiProvider).markDelivered(r.orderId), 'Marked delivered')),
          _iconBtn('Failed', Icons.report_gmailerrorred_outlined, Bo.danger, () => _fail(context, ref, r)),
        ];
      default:
        return const [];
    }
  }

  Widget _btn(String label, IconData icon, VoidCallback onTap) => Padding(
        padding: const EdgeInsets.only(left: 6),
        child: OutlinedButton.icon(
          onPressed: onTap,
          icon: Icon(icon, size: 16),
          label: Text(label),
          style: OutlinedButton.styleFrom(visualDensity: VisualDensity.compact),
        ),
      );

  Widget _iconBtn(String tip, IconData icon, Color color, VoidCallback onTap) => IconButton(
        tooltip: tip,
        visualDensity: VisualDensity.compact,
        icon: Icon(icon, size: 18, color: color),
        onPressed: onTap,
      );

  Future<void> _act(BuildContext context, WidgetRef ref, Future<void> Function() action, String ok) async {
    try {
      await action();
      ref.read(deliveryBoardProvider.notifier).refresh();
      if (context.mounted) AppToast.show(context, ok);
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  Future<void> _assign(BuildContext context, WidgetRef ref, DeliveryBoardRow r) async {
    final riders = await ref.read(ridersProvider.future);
    final active = riders.where((x) => x.isActive).toList();
    if (!context.mounted) return;
    if (active.isEmpty) {
      AppToast.show(context, 'No active riders. Add one under Delivery → Riders.', type: ToastType.error);
      return;
    }

    String? selectedId = r.riderId;
    final picked = await showDialog<String>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text('Assign rider · ${r.orderNumber}'),
          content: DropdownButtonFormField<String>(
            initialValue: active.any((x) => x.id == selectedId) ? selectedId : null,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Rider'),
            items: [
              for (final x in active)
                DropdownMenuItem(value: x.id, child: Text('${x.name} · ${x.phone}')),
            ],
            onChanged: (v) => setState(() => selectedId = v),
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
            FilledButton(
              onPressed: () => selectedId == null ? null : Navigator.pop(ctx, selectedId),
              child: const Text('Assign'),
            ),
          ],
        ),
      ),
    );

    if (picked == null || !context.mounted) return;
    await _act(context, ref, () => ref.read(staffApiProvider).assignRider(r.orderId, picked), 'Rider assigned');
  }

  Future<void> _fail(BuildContext context, WidgetRef ref, DeliveryBoardRow r) async {
    final reason = await _reasonDialog(context, 'Mark delivery failed', 'Reason (optional)');
    if (reason == null || !context.mounted) return; // cancelled
    await _act(context, ref, () => ref.read(staffApiProvider).markDeliveryFailed(r.orderId, reason.isEmpty ? null : reason), 'Marked failed');
  }

  Future<void> _cancel(BuildContext context, WidgetRef ref, DeliveryBoardRow r) async {
    final reason = await _reasonDialog(context, 'Cancel delivery · ${r.orderNumber}', 'Reason (optional)');
    if (reason == null || !context.mounted) return;
    await _act(context, ref, () => ref.read(staffApiProvider).cancelDelivery(r.orderId, reason.isEmpty ? null : reason), 'Delivery cancelled');
  }

  Future<String?> _reasonDialog(BuildContext context, String title, String label) {
    final ctrl = TextEditingController();
    return showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(title),
        content: TextField(controller: ctrl, decoration: InputDecoration(labelText: label), autofocus: true),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Back')),
          FilledButton(onPressed: () => Navigator.pop(ctx, ctrl.text.trim()), child: const Text('Confirm')),
        ],
      ),
    );
  }
}
