import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../core/models/dtos.dart';
import '../../core/models/enums.dart';
import '../../core/providers/providers.dart';
import '../../core/widgets/async_view.dart';
import '../../core/widgets/format.dart';
import '../../core/widgets/responsive.dart';
import 'floor_action_sheet.dart';
import 'floor_providers.dart';

Color tableColor(DerivedTableStatus s) {
  switch (s) {
    case DerivedTableStatus.available:
      return Colors.green;
    case DerivedTableStatus.occupied:
      return Colors.orange;
    case DerivedTableStatus.waitingPayment:
      return Colors.red;
    case DerivedTableStatus.reserved:
      return Colors.blue;
    default:
      return Colors.grey;
  }
}

class FloorScreen extends ConsumerWidget {
  const FloorScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final console = ref.watch(consoleProvider);
    final filter = ref.watch(floorStatusFilterProvider);
    final mineOnly = ref.watch(floorMineOnlyProvider);
    final myName = ref.watch(authControllerProvider).user?.fullName;

    final expanded = context.isExpanded;
    final selectedId = ref.watch(selectedFloorTableProvider);

    return AsyncView(
      value: console,
      onRetry: () => ref.invalidate(consoleProvider),
      data: (c) {
        var tables = c.floor;
        if (filter != null) tables = tables.where((t) => t.status == filter).toList();
        if (mineOnly && myName != null) {
          tables = tables.where((t) => t.waiterName == myName).toList();
        }

        void onTapTable(TableOverviewRow t) {
          if (expanded) {
            ref.read(selectedFloorTableProvider.notifier).state = t.tableId;
          } else {
            showFloorActionSheet(context, ref, t);
          }
        }

        final grid = tables.isEmpty
            ? const EmptyState(
                icon: Icons.table_restaurant,
                title: 'No tables',
                description: 'No tables match this filter.')
            : RefreshIndicator(
                onRefresh: () => refreshConsole(ref),
                child: LayoutBuilder(builder: (ctx, box) {
                  final cols = gridColumns(box.maxWidth, 180);
                  return GridView.builder(
                    padding: const EdgeInsets.all(12),
                    gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: cols,
                      childAspectRatio: 1.3,
                      crossAxisSpacing: 10,
                      mainAxisSpacing: 10,
                    ),
                    itemCount: tables.length,
                    itemBuilder: (_, i) => _TableCard(
                      t: tables[i],
                      selected: expanded && tables[i].tableId == selectedId,
                      onTap: () => onTapTable(tables[i]),
                    ),
                  );
                }),
              );

        final body = expanded
            ? Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(child: grid),
                  const VerticalDivider(width: 1),
                  SizedBox(
                    width: 320,
                    child: _SidePanel(
                      table: _find(tables, selectedId),
                      parentContext: context,
                      ref: ref,
                    ),
                  ),
                ],
              )
            : grid;

        return Column(
          children: [
            _FilterBar(filter: filter, mineOnly: mineOnly),
            Expanded(child: body),
          ],
        );
      },
    );
  }

  static TableOverviewRow? _find(List<TableOverviewRow> tables, String? id) {
    if (id == null) return null;
    for (final t in tables) {
      if (t.tableId == id) return t;
    }
    return null;
  }
}

/// Expanded-width side panel: shows the selected table's actions (mirrors the
/// web `.wc-actions` sticky panel).
class _SidePanel extends StatelessWidget {
  final TableOverviewRow? table;
  final BuildContext parentContext;
  final WidgetRef ref;
  const _SidePanel({required this.table, required this.parentContext, required this.ref});

  @override
  Widget build(BuildContext context) {
    if (table == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Text('Select a table to see actions',
              textAlign: TextAlign.center, style: TextStyle(color: Colors.grey.shade600)),
        ),
      );
    }
    return SingleChildScrollView(
      child: FloorActions(t: table!, parentContext: parentContext, ref: ref),
    );
  }
}

class _FilterBar extends ConsumerWidget {
  final DerivedTableStatus? filter;
  final bool mineOnly;
  const _FilterBar({required this.filter, required this.mineOnly});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    Widget chip(String label, DerivedTableStatus? value) => Padding(
          padding: const EdgeInsets.only(right: 6),
          child: ChoiceChip(
            label: Text(label),
            selected: filter == value,
            onSelected: (_) => ref.read(floorStatusFilterProvider.notifier).state = value,
          ),
        );

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      child: Row(children: [
        chip('All', null),
        chip('Available', DerivedTableStatus.available),
        chip('Occupied', DerivedTableStatus.occupied),
        chip('Waiting payment', DerivedTableStatus.waitingPayment),
        chip('Reserved', DerivedTableStatus.reserved),
        const SizedBox(width: 8),
        FilterChip(
          label: const Text('My tables'),
          selected: mineOnly,
          onSelected: (v) => ref.read(floorMineOnlyProvider.notifier).state = v,
        ),
      ]),
    );
  }
}

class _TableCard extends StatelessWidget {
  final TableOverviewRow t;
  final VoidCallback onTap;
  final bool selected;
  const _TableCard({required this.t, required this.onTap, this.selected = false});

  @override
  Widget build(BuildContext context) {
    final color = tableColor(t.status);
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          // Uniform border (radius requires uniform colours); selection just thickens/recolours it.
          border: Border.all(
              color: selected ? const Color(0xFFCB3A1A) : Colors.grey.shade200,
              width: selected ? 2 : 1),
          boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.05), blurRadius: 5)],
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Container(height: 4, color: color), // status accent strip
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(t.tableNumber,
                    style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.14),
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: Text(t.status.label,
                      style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: color)),
                ),
              ],
            ),
            const SizedBox(height: 6),
            Wrap(spacing: 10, runSpacing: 2, children: [
              Text('👥 ${t.guestCount?.toString() ?? '—'}/${t.capacity}',
                  style: const TextStyle(fontSize: 12)),
              if (t.sessionMinutes != null)
                Text('⏱ ${t.sessionMinutes}m', style: const TextStyle(fontSize: 12)),
              if (t.orderCount > 0)
                Text('🧾 ${t.orderCount}', style: const TextStyle(fontSize: 12)),
            ]),
            const Spacer(),
            if (t.currentBill > 0)
              Text(money(t.currentBill, currency: t.currency, twoDp: true),
                  style: TextStyle(
                      fontSize: 15, fontWeight: FontWeight.w800, color: color.withValues(alpha: 1))),
            if (t.waiterName != null)
              Text(t.waiterName!,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 11, color: Colors.grey.shade600)),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
