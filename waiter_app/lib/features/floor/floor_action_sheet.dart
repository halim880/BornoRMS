// parentContext is the long-lived floor-screen context (alive for the sheet's
// whole lifetime), so the async-gap heuristic doesn't apply here.
// ignore_for_file: use_build_context_synchronously
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_exception.dart';
import '../../core/auth/auth_controller.dart';
import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/widgets/pdf_opener.dart';
import '../../core/widgets/snack.dart';
import 'dialogs.dart';
import 'floor_screen.dart' show tableColor;
import 'session_bill_sheet.dart';

void showFloorActionSheet(BuildContext context, WidgetRef ref, TableOverviewRow t) {
  showModalBottomSheet(
    context: context,
    showDragHandle: true,
    builder: (_) => SafeArea(
      child: SingleChildScrollView(
        child: FloorActions(t: t, parentContext: context, ref: ref, closeOnTap: true),
      ),
    ),
  );
}

/// Wraps an action with snackbar error handling + a console refresh on success.
Future<void> _run(BuildContext ctx, WidgetRef ref, Future<void> Function() action,
    {String? success}) async {
  try {
    await action();
    if (ctx.mounted && success != null) showInfo(ctx, success);
    await refreshConsole(ref);
  } on ApiException catch (e) {
    if (ctx.mounted) showError(ctx, e.message);
  }
}

/// The table action list. Reused by the bottom sheet (compact: [closeOnTap] pops
/// the sheet before acting) and the floor side panel (expanded: acts in place).
class FloorActions extends StatelessWidget {
  final TableOverviewRow t;
  final BuildContext parentContext;
  final WidgetRef ref;
  final bool closeOnTap;
  const FloorActions({
    super.key,
    required this.t,
    required this.parentContext,
    required this.ref,
    this.closeOnTap = false,
  });

  @override
  Widget build(BuildContext context) {
    final api = ref.read(waiterApiProvider);
    final canClose = ref.read(authControllerProvider).user?.canCloseSession ?? false;
    final color = tableColor(t.status);

    Widget tile(IconData icon, String label, VoidCallback onTap, {Color? c}) => ListTile(
          leading: Icon(icon, color: c),
          title: Text(label, style: TextStyle(color: c)),
          dense: true,
          onTap: () {
            if (closeOnTap) Navigator.pop(context); // close the sheet first
            onTap();
          },
        );

    final children = <Widget>[
      Padding(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
        child: Row(children: [
          Text('Table ${t.tableNumber}',
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          const SizedBox(width: 10),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
            decoration: BoxDecoration(
                color: color.withValues(alpha: 0.14), borderRadius: BorderRadius.circular(999)),
            child: Text(t.status.label,
                style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: color)),
          ),
        ]),
      ),
      const Divider(height: 1),
    ];

    if (t.sessionId == null) {
      children.add(tile(Icons.play_circle_outline, 'Open table', () async {
        final guests = await guestCountDialog(parentContext, t.guestCount ?? 0);
        if (guests == null) return;
        await _run(parentContext, ref, () => api.openSession(t.tableId, guests),
            success: 'Table ${t.tableNumber} opened');
      }));
    } else {
      final sid = t.sessionId!;
      children.addAll([
        tile(Icons.add_shopping_cart, 'Take order', () {
          ref.read(takeOrderTargetProvider.notifier).state = TakeOrderTarget(
            tableId: t.tableId,
            tableNumber: t.tableNumber,
            sessionId: sid,
            guests: t.guestCount ?? 0,
          );
          ref.read(selectedTabProvider.notifier).state = 1;
        }),
        tile(Icons.receipt_long, 'View bill', () => showSessionBill(parentContext, ref, sid)),
        tile(Icons.payments_outlined, 'Request payment', () async {
          await _run(parentContext, ref, () => api.requestPayment(sid),
              success: 'Payment requested for Table ${t.tableNumber}');
        }),
        tile(Icons.people_outline, 'Guest count', () async {
          final g = await guestCountDialog(parentContext, t.guestCount ?? 0);
          if (g == null) return;
          await _run(parentContext, ref, () => api.changeGuests(sid, g), success: 'Guest count updated');
        }),
        tile(Icons.swap_horiz, 'Move / transfer table', () async {
          final target =
              await pickTableDialog(parentContext, ref, 'Move to which table?', t.tableId);
          if (target == null) return;
          await _run(parentContext, ref, () => api.moveTable(sid, target.id),
              success: 'Moved to Table ${target.tableNumber}');
        }),
        tile(Icons.merge, 'Merge tables', () async {
          final floor = ref.read(consoleProvider).valueOrNull?.floor ?? const [];
          final choice = await mergeDialog(parentContext, t, floor);
          if (choice == null) return;
          await _run(parentContext, ref, () => api.mergeSessions(sid, choice.sourceSessionIds),
              success: 'Tables merged');
        }),
        tile(Icons.call_split, 'Split session', () async {
          final choice = await splitDialog(parentContext, ref, t);
          if (choice == null) return;
          await _run(
              parentContext,
              ref,
              () => api.splitSession(sid, choice.orderIds, choice.targetTableId, choice.guests),
              success: 'Session split');
        }),
        tile(Icons.badge_outlined, 'Transfer waiter', () async {
          final choice = await transferWaiterDialog(parentContext, ref);
          if (choice == null) return;
          await _run(parentContext, ref, () => api.transferWaiter(sid, choice.userId, choice.name),
              success: 'Transferred to ${choice.name}');
        }),
        tile(Icons.print_outlined, 'Print KOT', () {
          if (t.orderId == null) {
            showError(parentContext, 'No order to print.');
            return;
          }
          openOrderPdf(parentContext, ref,
              orderId: t.orderId!, orderNumber: t.orderNumber ?? 'order', kot: true);
        }),
        tile(Icons.receipt, 'Print bill', () {
          if (t.orderId == null) {
            showError(parentContext, 'No order to print.');
            return;
          }
          openOrderPdf(parentContext, ref,
              orderId: t.orderId!, orderNumber: t.orderNumber ?? 'order', kot: false);
        }),
        if (canClose)
          tile(Icons.lock_outline, 'Close session', () async {
            final ok = await confirmDialog(parentContext, 'Close session',
                'Close the session at Table ${t.tableNumber}? All orders must be settled.',
                confirmLabel: 'Close', danger: true);
            if (!ok) return;
            await _run(parentContext, ref, () => api.closeSession(sid),
                success: 'Session closed');
          }, c: Colors.red.shade700),
      ]);
    }

    return Column(mainAxisSize: MainAxisSize.min, children: children);
  }
}
