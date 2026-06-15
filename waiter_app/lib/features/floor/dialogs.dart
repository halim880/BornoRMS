import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/models/enums.dart';
import '../../core/providers/providers.dart';
import '../../core/widgets/format.dart';

// ---------- guest count ----------
Future<int?> guestCountDialog(BuildContext context, int initial) {
  final ctrl = TextEditingController(text: initial > 0 ? '$initial' : '');
  return showDialog<int>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: const Text('Guest count'),
      content: TextField(
        controller: ctrl,
        keyboardType: TextInputType.number,
        autofocus: true,
        decoration: const InputDecoration(labelText: 'Number of guests'),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
        FilledButton(
          onPressed: () => Navigator.pop(ctx, int.tryParse(ctrl.text.trim()) ?? 0),
          child: const Text('Save'),
        ),
      ],
    ),
  );
}

// ---------- pick a table (move / split target) ----------
Future<RestaurantTable?> pickTableDialog(
    BuildContext context, WidgetRef ref, String prompt, String excludeTableId) async {
  final tables = await ref.read(tablesProvider.future);
  final options = tables.where((t) => t.id != excludeTableId).toList();
  if (!context.mounted) return null;
  return showDialog<RestaurantTable>(
    context: context,
    builder: (ctx) => SimpleDialog(
      title: Text(prompt),
      children: [
        if (options.isEmpty)
          const Padding(padding: EdgeInsets.all(16), child: Text('No other tables available.')),
        ...options.map((t) => SimpleDialogOption(
              onPressed: () => Navigator.pop(ctx, t),
              child: Text('Table ${t.tableNumber}  ·  ${t.capacity} seats'),
            )),
      ],
    ),
  );
}

// ---------- merge ----------
class MergeChoice {
  final List<String> sourceSessionIds;
  MergeChoice(this.sourceSessionIds);
}

Future<MergeChoice?> mergeDialog(
    BuildContext context, TableOverviewRow survivor, List<TableOverviewRow> allRows) {
  final candidates = allRows
      .where((r) =>
          r.sessionId != null &&
          r.sessionId != survivor.sessionId &&
          (r.status == DerivedTableStatus.occupied ||
              r.status == DerivedTableStatus.waitingPayment))
      .toList();
  final selected = <String>{};

  return showDialog<MergeChoice>(
    context: context,
    builder: (ctx) => StatefulBuilder(
      builder: (ctx, setState) => AlertDialog(
        title: Text('Merge into Table ${survivor.tableNumber}'),
        content: SizedBox(
          width: double.maxFinite,
          child: candidates.isEmpty
              ? const Text('No other active tables to merge.')
              : ListView(
                  shrinkWrap: true,
                  children: candidates
                      .map((c) => CheckboxListTile(
                            value: selected.contains(c.sessionId),
                            title: Text('Table ${c.tableNumber}'),
                            subtitle: Text(money(c.currentBill, currency: c.currency, twoDp: true)),
                            onChanged: (v) => setState(() {
                              if (v == true) {
                                selected.add(c.sessionId!);
                              } else {
                                selected.remove(c.sessionId);
                              }
                            }),
                          ))
                      .toList(),
                ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
          FilledButton(
            onPressed: selected.isEmpty
                ? null
                : () => Navigator.pop(ctx, MergeChoice(selected.toList())),
            child: const Text('Merge'),
          ),
        ],
      ),
    ),
  );
}

// ---------- split ----------
class SplitChoice {
  final List<String> orderIds;
  final String targetTableId;
  final int guests;
  SplitChoice(this.orderIds, this.targetTableId, this.guests);
}

Future<SplitChoice?> splitDialog(
    BuildContext context, WidgetRef ref, TableOverviewRow source) async {
  final bill = await ref.read(sessionBillProvider(source.sessionId!).future);
  final tables = await ref.read(tablesProvider.future);
  final targets = tables.where((t) => t.id != source.tableId).toList();
  if (!context.mounted) return null;

  final selected = <String>{};
  String? targetId;

  return showDialog<SplitChoice>(
    context: context,
    builder: (ctx) => StatefulBuilder(
      builder: (ctx, setState) => AlertDialog(
        title: Text('Split from Table ${source.tableNumber}'),
        content: SizedBox(
          width: double.maxFinite,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Orders to move', style: TextStyle(fontWeight: FontWeight.bold)),
              Flexible(
                child: ListView(
                  shrinkWrap: true,
                  children: bill.orders
                      .map((o) => CheckboxListTile(
                            value: selected.contains(o.orderId),
                            title: Text(o.orderNumber),
                            subtitle: Text(money(o.orderTotal, currency: bill.currency, twoDp: true)),
                            onChanged: (v) => setState(() {
                              if (v == true) {
                                selected.add(o.orderId);
                              } else {
                                selected.remove(o.orderId);
                              }
                            }),
                          ))
                      .toList(),
                ),
              ),
              const SizedBox(height: 8),
              const Text('Target table', style: TextStyle(fontWeight: FontWeight.bold)),
              DropdownButton<String>(
                isExpanded: true,
                value: targetId,
                hint: const Text('Choose a free table'),
                items: targets
                    .map((t) => DropdownMenuItem(value: t.id, child: Text('Table ${t.tableNumber}')))
                    .toList(),
                onChanged: (v) => setState(() => targetId = v),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
          FilledButton(
            onPressed: (selected.isEmpty || targetId == null)
                ? null
                : () => Navigator.pop(ctx, SplitChoice(selected.toList(), targetId!, 0)),
            child: const Text('Split'),
          ),
        ],
      ),
    ),
  );
}

// ---------- transfer waiter ----------
class TransferChoice {
  final String userId;
  final String name;
  TransferChoice(this.userId, this.name);
}

Future<TransferChoice?> transferWaiterDialog(BuildContext context, WidgetRef ref) async {
  final staff = await ref.read(staffProvider.future);
  final waiters = staff
      .where((s) =>
          s.isActive &&
          s.roles.any((r) =>
              r == 'Waiter' || r == 'Manager' || r == 'Admin' || r == 'SuperAdmin'))
      .toList();
  if (!context.mounted) return null;

  return showDialog<TransferChoice>(
    context: context,
    builder: (ctx) => SimpleDialog(
      title: const Text('Transfer to waiter'),
      children: [
        if (waiters.isEmpty)
          const Padding(padding: EdgeInsets.all(16), child: Text('No staff available.')),
        ...waiters.map((s) => SimpleDialogOption(
              onPressed: () => Navigator.pop(ctx, TransferChoice(s.id, s.fullName)),
              child: Text(s.fullName),
            )),
      ],
    ),
  );
}

// ---------- confirm ----------
Future<bool> confirmDialog(BuildContext context, String title, String message,
    {String confirmLabel = 'Confirm', bool danger = false}) async {
  final ok = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text(title),
      content: Text(message),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
        FilledButton(
          style: danger ? FilledButton.styleFrom(backgroundColor: Colors.red.shade700) : null,
          onPressed: () => Navigator.pop(ctx, true),
          child: Text(confirmLabel),
        ),
      ],
    ),
  );
  return ok ?? false;
}
