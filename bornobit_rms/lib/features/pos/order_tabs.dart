import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_colors.dart';
import '../dashboard/widgets.dart' show money;
import 'pos_dialogs.dart';
import 'pos_models.dart';
import 'pos_providers.dart';
import 'pos_section.dart';

/// Badge for an order chip: first letter of the type + the order-number's
/// trailing sequence with leading zeros stripped (ORD-...-0002 → "D2").
String orderBadge(ActiveOrder o) {
  final seg = o.orderNumber.split('-').last;
  final n = int.tryParse(seg);
  final seq = n?.toString() ?? seg;
  final t = o.orderType.isNotEmpty ? o.orderType[0].toUpperCase() : '?';
  return '$t$seq';
}

class OrderTabs extends ConsumerWidget {
  const OrderTabs({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final a = context.appColors;
    final orders = ref.watch(posActiveOrdersProvider).valueOrNull ?? const [];
    final activeId = ref.watch(posControllerProvider).orderId;

    return PosPanel(
      padding: const EdgeInsets.all(10),
      child: SizedBox(
        height: 64,
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // leading "+" tile — stretches to the chip height
            _AddTile(
              onTap: () =>
                  showDialog(context: context, builder: (_) => const NewOrderDialog()),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: orders.isEmpty
                  ? Align(
                      alignment: Alignment.centerLeft,
                      child: Text(
                        'No open orders — tap + to start one.',
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                    )
                  : ListView.separated(
                      scrollDirection: Axis.horizontal,
                      itemCount: orders.length,
                      separatorBuilder: (_, __) => const SizedBox(width: 8),
                      itemBuilder: (_, i) => _OrderChip(
                        order: orders[i],
                        active: orders[i].id == activeId,
                        onTap: () => ref
                            .read(posControllerProvider.notifier)
                            .selectOrder(orders[i].id),
                      ),
                    ),
            ),
            const SizedBox(width: 8),
            const _ShiftButton(),
            const SizedBox(width: 4),
            IconButton(
              tooltip: 'Printer settings',
              icon: Icon(Icons.print_outlined, color: a.textTertiary),
              onPressed: () => showDialog(
                  context: context, builder: (_) => const PrinterSettingsDialog()),
            ),
          ],
        ),
      ),
    );
  }
}

/// Drawer/shift status pill — green when a shift is open, muted "Open shift" when not.
/// Tapping opens the [ShiftDialog] (open when closed, close when open).
class _ShiftButton extends ConsumerWidget {
  const _ShiftButton();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final a = context.appColors;
    final drawer = ref.watch(posDrawerProvider).valueOrNull;
    final open = drawer?.isOpen ?? false;

    return Tooltip(
      message: open ? 'Shift ${drawer!.drawerNumber} — tap to close' : 'No open shift — tap to open',
      child: Material(
        color: open ? a.successTint : a.surfaceMuted,
        borderRadius: BorderRadius.circular(AppColors.radiusChip),
        child: InkWell(
          borderRadius: BorderRadius.circular(AppColors.radiusChip),
          onTap: () => showDialog(context: context, builder: (_) => const ShiftDialog()),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(open ? Icons.point_of_sale : Icons.lock_outline,
                    size: 18, color: open ? a.success : a.textTertiary),
                const SizedBox(width: 6),
                Text(
                  open ? 'Shift open' : 'Open shift',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: open ? a.success : a.textSecondary,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _AddTile extends StatelessWidget {
  final VoidCallback onTap;
  const _AddTile({required this.onTap});

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    return Material(
      color: a.accent,
      borderRadius: BorderRadius.circular(AppColors.radiusChip),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppColors.radiusChip),
        onTap: onTap,
        child: SizedBox(
          width: 56,
          child: Icon(Icons.add, color: a.onAccent),
        ),
      ),
    );
  }
}

class _OrderChip extends StatelessWidget {
  final ActiveOrder order;
  final bool active;
  final VoidCallback onTap;
  const _OrderChip({required this.order, required this.active, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    final text = Theme.of(context).textTheme;

    return Material(
      color: active ? a.accentTint : a.surface,
      borderRadius: BorderRadius.circular(AppColors.radiusChip),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppColors.radiusChip),
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(AppColors.radiusChip),
            border: Border.all(
              color: active ? a.accent : a.borderStrong,
              width: active ? 1.5 : 1,
            ),
            boxShadow: active
                ? [BoxShadow(color: a.accent.withValues(alpha: 0.18), blurRadius: 0, spreadRadius: 2)]
                : null,
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              // badge
              Container(
                width: 30,
                height: 30,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: active ? a.accent : a.surfaceMuted,
                  borderRadius: BorderRadius.circular(8),
                  border: active ? null : Border.all(color: a.border),
                ),
                child: Text(
                  orderBadge(order),
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                    color: active ? a.onAccent : a.textSecondary,
                  ),
                ),
              ),
              const SizedBox(width: 10),
              Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    money(order.total, order.currency),
                    style: AppColors.priceText.copyWith(color: a.textPrimary),
                  ),
                  Text('${order.itemCount} items', style: text.bodySmall),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
