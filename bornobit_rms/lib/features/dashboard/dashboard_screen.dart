import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import 'charts.dart';
import 'widgets.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(dashboardProvider);

    return async.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => _ErrorView(message: e.toString(), onRetry: () => ref.invalidate(dashboardProvider)),
      data: (d) => _DashboardBody(data: d),
    );
  }
}

class _DashboardBody extends ConsumerWidget {
  final DashboardData data;
  const _DashboardBody({required this.data});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final range = ref.watch(dashboardRangeProvider);
    final cur = data.summary.currency;

    return LayoutBuilder(
      builder: (context, c) {
        final w = c.maxWidth;
        // Column count for the KPI row + section grids.
        final kpiCols = w >= 1100 ? 4 : (w >= 760 ? 2 : 1);
        final halfCols = w >= 980 ? 2 : 1;
        final triCols = w >= 980 ? 3 : (w >= 640 ? 2 : 1);

        return RefreshIndicator(
          onRefresh: () => ref.read(dashboardProvider.notifier).refresh(),
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              // Range selector
              Row(
                children: [
                  const Text('Overview',
                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: Bo.text)),
                  const Spacer(),
                  _RangeSelector(
                    selected: range,
                    onChanged: (r) {
                      ref.read(dashboardRangeProvider.notifier).state = r;
                      ref.read(dashboardProvider.notifier).refresh();
                    },
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // Section 1: KPI cards
              _Grid(columns: kpiCols, ratioMinHeight: 150, children: [
                KpiCard(
                  label: "Today's Sales",
                  value: money(data.summary.todayRevenue, cur),
                  icon: Icons.payments_outlined,
                  tint: Bo.primarySoft,
                  stats: [
                    MiniStat('orders', count(data.summary.todayOrderCount), tone: 'info'),
                    MiniStat('avg', money(data.summary.averageOrderValue, cur), tone: 'neutral'),
                  ],
                ),
                KpiCard(
                  label: 'Tables',
                  value: '${data.summary.occupiedTables} occupied',
                  icon: Icons.table_restaurant_outlined,
                  tint: Bo.successSoft,
                  stats: [
                    MiniStat('free', count(data.summary.availableTables), tone: 'success'),
                    MiniStat('reserved', count(data.summary.reservedTables), tone: 'info'),
                    MiniStat('to pay', count(data.summary.waitingPaymentTables), tone: 'warning'),
                  ],
                ),
                KpiCard(
                  label: 'Kitchen',
                  value: '${data.summary.pendingOrders} pending',
                  icon: Icons.soup_kitchen_outlined,
                  tint: Bo.warningSoft,
                  stats: [
                    MiniStat('preparing', count(data.summary.preparingOrders), tone: 'warning'),
                    MiniStat('ready', count(data.summary.readyOrders), tone: 'primary'),
                  ],
                ),
                KpiCard(
                  label: 'Customer Activity',
                  value: '${data.summary.activeDiningSessions} sessions',
                  icon: Icons.groups_outlined,
                  tint: Bo.infoSoft,
                  stats: [
                    MiniStat('QR', count(data.summary.qrOrdersToday), tone: 'info'),
                    MiniStat('staff', count(data.summary.walkInOrdersToday), tone: 'neutral'),
                  ],
                ),
              ]),
              const SizedBox(height: 16),

              // Section 2: live floor
              SectionCard(
                title: 'Live floor',
                icon: Icons.grid_view_outlined,
                child: _LiveFloor(tables: data.tables),
              ),
              const SizedBox(height: 16),

              // Section 3: analytics
              _Grid(columns: triCols, ratioMinHeight: 280, children: [
                SectionCard(title: 'Sales by hour', icon: Icons.show_chart, child: SalesByHourChart(data: data.salesByHour)),
                SectionCard(title: 'Sales by category', icon: Icons.pie_chart_outline, child: SalesByCategoryChart(data: data.salesByCategory)),
                SectionCard(title: 'Top selling items', icon: Icons.local_fire_department_outlined, child: TopItemsChart(data: data.topItems)),
              ]),
              const SizedBox(height: 16),

              // Section 4: live orders
              SectionCard(
                title: 'Live orders',
                icon: Icons.receipt_long_outlined,
                trailing: Text('${data.orders.totalCount} total', style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                child: _LiveOrders(orders: data.orders.items, currency: cur),
              ),
              const SizedBox(height: 16),

              // Section 5 + 6: kitchen perf + requests
              _Grid(columns: halfCols, ratioMinHeight: 220, children: [
                SectionCard(title: 'Kitchen performance', icon: Icons.timer_outlined, child: _KitchenPerf(k: data.kitchen)),
                SectionCard(title: 'Customer requests', icon: Icons.notifications_active_outlined, child: _Requests(requests: data.requests)),
              ]),
              const SizedBox(height: 16),

              // Section 7: inventory alerts
              SectionCard(
                title: 'Inventory alerts',
                icon: Icons.inventory_2_outlined,
                child: _InventoryAlerts(inv: data.inventory, columns: triCols),
              ),
              const SizedBox(height: 16),

              // Section 8 + 9: staff leaderboard + revenue breakdown
              _Grid(columns: halfCols, ratioMinHeight: 240, children: [
                SectionCard(title: 'Staff leaderboard', icon: Icons.emoji_events_outlined, child: _StaffLeaderboard(staff: data.staff, currency: cur)),
                SectionCard(title: 'Revenue breakdown', icon: Icons.account_balance_wallet_outlined, child: _RevenueBreakdown(r: data.revenue)),
              ]),
            ],
          ),
        );
      },
    );
  }
}

/// A simple responsive grid: lays children into [columns] equal-width cells.
class _Grid extends StatelessWidget {
  final int columns;
  final double ratioMinHeight;
  final List<Widget> children;
  const _Grid({required this.columns, required this.children, this.ratioMinHeight = 150});

  @override
  Widget build(BuildContext context) {
    const gap = 16.0;
    return LayoutBuilder(builder: (context, c) {
      final cellW = (c.maxWidth - gap * (columns - 1)) / columns;
      return Wrap(
        spacing: gap,
        runSpacing: gap,
        children: [
          for (final child in children)
            SizedBox(width: cellW, child: child),
        ],
      );
    });
  }
}

class _RangeSelector extends StatelessWidget {
  final DashboardRange selected;
  final ValueChanged<DashboardRange> onChanged;
  const _RangeSelector({required this.selected, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return SegmentedButton<DashboardRange>(
      showSelectedIcon: false,
      style: ButtonStyle(
        visualDensity: VisualDensity.compact,
        textStyle: WidgetStatePropertyAll(const TextStyle(fontSize: 12)),
      ),
      segments: [
        for (final r in DashboardRange.values)
          ButtonSegment(value: r, label: Text(r.label)),
      ],
      selected: {selected},
      onSelectionChanged: (s) => onChanged(s.first),
    );
  }
}

// ---- section bodies ----
class _LiveFloor extends StatelessWidget {
  final List<TableOverviewRow> tables;
  const _LiveFloor({required this.tables});

  @override
  Widget build(BuildContext context) {
    if (tables.isEmpty) return const _EmptyHint('No active tables');
    return Wrap(
      spacing: 10,
      runSpacing: 10,
      children: [
        for (final t in tables)
          Container(
            width: 150,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Bo.slate50,
              borderRadius: BorderRadius.circular(Bo.radiusMd),
              border: Border.all(color: Bo.border),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Table ${t.tableNumber}',
                        style: const TextStyle(fontWeight: FontWeight.w700, color: Bo.text)),
                    Text('${t.capacity}p', style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                  ],
                ),
                const SizedBox(height: 6),
                ToneChip(tableStatusLabel(t.status), tableStatusTone(t.status)),
                if (t.status != 'Available' && t.status != 'Reserved') ...[
                  const SizedBox(height: 8),
                  Text('${t.guestCount ?? 0} guests · ${t.sessionMinutes ?? 0}m',
                      style: const TextStyle(color: Bo.textMuted, fontSize: 12)),
                  Text(money(t.currentBill, t.currency),
                      style: const TextStyle(fontWeight: FontWeight.w700, color: Bo.text)),
                  if (t.waiterName != null)
                    Text(t.waiterName!, style: const TextStyle(color: Bo.textSubtle, fontSize: 11)),
                ],
              ],
            ),
          ),
      ],
    );
  }
}

class _LiveOrders extends StatelessWidget {
  final List<LiveOrderRow> orders;
  final String currency;
  const _LiveOrders({required this.orders, required this.currency});

  @override
  Widget build(BuildContext context) {
    if (orders.isEmpty) return const _EmptyHint('No orders yet');
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: DataTable(
        columnSpacing: 28,
        headingRowHeight: 40,
        dataRowMinHeight: 40,
        dataRowMaxHeight: 48,
        columns: const [
          DataColumn(label: Text('Order')),
          DataColumn(label: Text('Table')),
          DataColumn(label: Text('Source')),
          DataColumn(label: Text('Time')),
          DataColumn(label: Text('Amount')),
          DataColumn(label: Text('Status')),
        ],
        rows: [
          for (final o in orders)
            DataRow(cells: [
              DataCell(Text(o.orderNumber, style: const TextStyle(fontWeight: FontWeight.w600))),
              DataCell(Text(o.tableNumber ?? '—')),
              DataCell(Text('${o.orderType} · ${o.channel}')),
              DataCell(Text(shortDateTime(o.orderedAtUtc))),
              DataCell(Text(money(o.total, o.currency))),
              DataCell(ToneChip(o.status, orderStatusTone(o.status))),
            ]),
        ],
      ),
    );
  }
}

class _KitchenPerf extends StatelessWidget {
  final KitchenPerformance k;
  const _KitchenPerf({required this.k});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Wrap(spacing: 8, runSpacing: 8, children: [
          MiniStat('avg prep (min)', k.averagePrepMinutes.toStringAsFixed(1), tone: 'info'),
          MiniStat('completed', count(k.completedToday), tone: 'success'),
          MiniStat('waiting >10m', count(k.ordersWaitingOver10Min), tone: k.ordersWaitingOver10Min > 0 ? 'danger' : 'neutral'),
        ]),
        const SizedBox(height: 12),
        if (k.longestWaitingOrderNumber != null)
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(color: Bo.warningSoft, borderRadius: BorderRadius.circular(Bo.radiusSm)),
            child: Text('Longest waiting: ${k.longestWaitingOrderNumber} (${k.longestWaitingMinutes ?? 0} min)',
                style: const TextStyle(color: Bo.warning, fontWeight: FontWeight.w600, fontSize: 12)),
          ),
        const SizedBox(height: 12),
        const Text('Kitchen load', style: TextStyle(color: Bo.textSubtle, fontSize: 12, fontWeight: FontWeight.w600)),
        const SizedBox(height: 6),
        Wrap(spacing: 8, runSpacing: 8, children: [
          MiniStat('pending', count(k.pendingCount), tone: 'neutral'),
          MiniStat('preparing', count(k.preparingCount), tone: 'warning'),
          MiniStat('ready', count(k.readyCount), tone: 'primary'),
        ]),
      ],
    );
  }
}

class _Requests extends StatelessWidget {
  final List<CustomerRequestRow> requests;
  const _Requests({required this.requests});

  @override
  Widget build(BuildContext context) {
    if (requests.isEmpty) return const _EmptyHint('No pending requests');
    return Column(
      children: [
        for (final r in requests)
          ListTile(
            dense: true,
            contentPadding: EdgeInsets.zero,
            leading: const Icon(Icons.notifications_none, size: 20, color: Bo.textMuted),
            title: Text('Table ${r.tableNumber} · ${requestLabel(r.type)}',
                style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
            subtitle: r.note != null ? Text(r.note!, style: const TextStyle(fontSize: 12)) : null,
            trailing: ToneChip('${r.waitingMinutes}m', r.waitingMinutes > 5 ? 'danger' : 'neutral'),
          ),
      ],
    );
  }
}

class _InventoryAlerts extends StatelessWidget {
  final InventoryAlerts inv;
  final int columns;
  const _InventoryAlerts({required this.inv, required this.columns});

  @override
  Widget build(BuildContext context) {
    return _Grid(columns: columns, ratioMinHeight: 160, children: [
      _AlertList(
        title: 'Low stock',
        tone: 'warning',
        rows: [for (final i in inv.lowStock) '${i.name} — ${i.qtyOnHand} ${i.unitCode}'],
      ),
      _AlertList(
        title: 'Out of stock',
        tone: 'danger',
        rows: [for (final i in inv.outOfStock) i.name],
      ),
      _AlertList(
        title: "Today's consumption",
        tone: 'info',
        rows: [for (final i in inv.todaysConsumption) '${i.name} — ${i.qtyConsumed} ${i.unitCode}'],
      ),
    ]);
  }
}

class _AlertList extends StatelessWidget {
  final String title;
  final String tone;
  final List<String> rows;
  const _AlertList({required this.title, required this.tone, required this.rows});

  @override
  Widget build(BuildContext context) {
    final c = toneColors(tone);
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Bo.slate50,
        borderRadius: BorderRadius.circular(Bo.radiusMd),
        border: Border.all(color: Bo.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(children: [
            Container(width: 8, height: 8, decoration: BoxDecoration(color: c.fg, shape: BoxShape.circle)),
            const SizedBox(width: 6),
            Text(title, style: const TextStyle(fontWeight: FontWeight.w700, color: Bo.text, fontSize: 13)),
            const Spacer(),
            Text('${rows.length}', style: TextStyle(color: c.fg, fontWeight: FontWeight.w700)),
          ]),
          const SizedBox(height: 8),
          if (rows.isEmpty)
            const Text('None', style: TextStyle(color: Bo.textSubtle, fontSize: 12))
          else
            for (final r in rows.take(6))
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 2),
                child: Text(r, style: const TextStyle(fontSize: 12, color: Bo.textMuted), overflow: TextOverflow.ellipsis),
              ),
          if (rows.length > 6)
            Text('+${rows.length - 6} more', style: const TextStyle(fontSize: 11, color: Bo.textSubtle)),
        ],
      ),
    );
  }
}

class _StaffLeaderboard extends StatelessWidget {
  final List<StaffActivityRow> staff;
  final String currency;
  const _StaffLeaderboard({required this.staff, required this.currency});

  @override
  Widget build(BuildContext context) {
    if (staff.isEmpty) return const _EmptyHint('No staff activity today');
    return Column(
      children: [
        for (final s in staff.take(8))
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 5),
            child: Row(
              children: [
                Expanded(flex: 4, child: Text(s.waiterName, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 13, color: Bo.text))),
                Expanded(flex: 2, child: Text('${s.ordersProcessed} ord', style: const TextStyle(fontSize: 12, color: Bo.textSubtle))),
                Expanded(flex: 2, child: Text('${s.tablesAssigned} tbl', style: const TextStyle(fontSize: 12, color: Bo.textSubtle))),
                Expanded(flex: 3, child: Text(money(s.revenue, currency), textAlign: TextAlign.right, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: Bo.text))),
              ],
            ),
          ),
      ],
    );
  }
}

class _RevenueBreakdown extends StatelessWidget {
  final RevenueBreakdown r;
  const _RevenueBreakdown({required this.r});

  @override
  Widget build(BuildContext context) {
    Widget row(String label, double v, {bool bold = false}) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(label, style: TextStyle(fontSize: 13, color: bold ? Bo.text : Bo.textMuted, fontWeight: bold ? FontWeight.w700 : FontWeight.w400)),
              Text(money(v, r.currency), style: TextStyle(fontSize: 13, fontWeight: bold ? FontWeight.w800 : FontWeight.w600, color: Bo.text)),
            ],
          ),
        );

    return Column(
      children: [
        row('Dine-in', r.dineInRevenue),
        row('Takeaway', r.takeawayRevenue),
        row('Delivery', r.deliveryRevenue),
        row('QR ordering', r.qrOrderingRevenue),
        const Divider(),
        row('Discount', r.discountAmount),
        row('Tax collected', r.taxCollected),
        row('Service charge', r.serviceChargeCollected),
        const Divider(),
        row('Grand total', r.grandTotal, bold: true),
      ],
    );
  }
}

class _EmptyHint extends StatelessWidget {
  final String text;
  const _EmptyHint(this.text);
  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 24),
        child: Center(child: Text(text, style: const TextStyle(color: Bo.textSubtle))),
      );
}

class _ErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  const _ErrorView({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off, size: 40, color: Bo.textSubtle),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center, style: const TextStyle(color: Bo.textMuted)),
            const SizedBox(height: 16),
            FilledButton.icon(onPressed: onRetry, icon: const Icon(Icons.refresh), label: const Text('Retry')),
          ],
        ),
      ),
    );
  }
}
