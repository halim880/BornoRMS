import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/i18n/labels.dart';
import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import 'charts.dart';
import 'widgets.dart';

/// Localized label for a [DashboardRange].
String _rangeLabel(AppLocalizations t, DashboardRange r) => switch (r) {
      DashboardRange.today => t.rangeToday,
      DashboardRange.yesterday => t.rangeYesterday,
      DashboardRange.last7Days => t.rangeLast7Days,
      DashboardRange.thisMonth => t.rangeThisMonth,
    };

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
    final t = AppLocalizations.of(context);
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
                  Text(t.dashOverview,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: Bo.text)),
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
                  label: t.dashTodaysSales,
                  value: money(data.summary.todayRevenue, cur),
                  icon: Icons.payments_outlined,
                  tint: Bo.primarySoft,
                  stats: [
                    MiniStat(t.dashStatOrders, count(data.summary.todayOrderCount), tone: 'info'),
                    MiniStat(t.dashStatAvg, money(data.summary.averageOrderValue, cur), tone: 'neutral'),
                  ],
                ),
                KpiCard(
                  label: t.dashTables,
                  value: t.dashOccupiedValue(data.summary.occupiedTables),
                  icon: Icons.table_restaurant_outlined,
                  tint: Bo.successSoft,
                  stats: [
                    MiniStat(t.dashStatFree, count(data.summary.availableTables), tone: 'success'),
                    MiniStat(t.dashStatReserved, count(data.summary.reservedTables), tone: 'info'),
                    MiniStat(t.dashStatToPay, count(data.summary.waitingPaymentTables), tone: 'warning'),
                  ],
                ),
                KpiCard(
                  label: t.dashKitchen,
                  value: t.dashPendingValue(data.summary.pendingOrders),
                  icon: Icons.soup_kitchen_outlined,
                  tint: Bo.warningSoft,
                  stats: [
                    MiniStat(t.dashStatPreparing, count(data.summary.preparingOrders), tone: 'warning'),
                    MiniStat(t.dashStatReady, count(data.summary.readyOrders), tone: 'primary'),
                  ],
                ),
                KpiCard(
                  label: t.dashCustomerActivity,
                  value: t.dashSessionsValue(data.summary.activeDiningSessions),
                  icon: Icons.groups_outlined,
                  tint: Bo.infoSoft,
                  stats: [
                    MiniStat(t.dashStatQr, count(data.summary.qrOrdersToday), tone: 'info'),
                    MiniStat(t.dashStatStaff, count(data.summary.walkInOrdersToday), tone: 'neutral'),
                  ],
                ),
              ]),
              const SizedBox(height: 16),

              // Section 2: live floor
              SectionCard(
                title: t.dashLiveFloor,
                icon: Icons.grid_view_outlined,
                child: _LiveFloor(tables: data.tables),
              ),
              const SizedBox(height: 16),

              // Section 3: analytics
              _Grid(columns: triCols, ratioMinHeight: 280, children: [
                SectionCard(title: t.dashSalesByHour, icon: Icons.show_chart, child: SalesByHourChart(data: data.salesByHour)),
                SectionCard(title: t.dashSalesByCategory, icon: Icons.pie_chart_outline, child: SalesByCategoryChart(data: data.salesByCategory)),
                SectionCard(title: t.dashTopItems, icon: Icons.local_fire_department_outlined, child: TopItemsChart(data: data.topItems)),
              ]),
              const SizedBox(height: 16),

              // Section 4: live orders
              SectionCard(
                title: t.dashLiveOrders,
                icon: Icons.receipt_long_outlined,
                trailing: Text(t.dashTotalSuffix(data.orders.totalCount), style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                child: _LiveOrders(orders: data.orders.items, currency: cur),
              ),
              const SizedBox(height: 16),

              // Section 5 + 6: kitchen perf + requests
              _Grid(columns: halfCols, ratioMinHeight: 220, children: [
                SectionCard(title: t.dashKitchenPerf, icon: Icons.timer_outlined, child: _KitchenPerf(k: data.kitchen)),
                SectionCard(title: t.dashCustomerRequests, icon: Icons.notifications_active_outlined, child: _Requests(requests: data.requests)),
              ]),
              const SizedBox(height: 16),

              // Section 7: inventory alerts
              SectionCard(
                title: t.dashInventoryAlerts,
                icon: Icons.inventory_2_outlined,
                child: _InventoryAlerts(inv: data.inventory, columns: triCols),
              ),
              const SizedBox(height: 16),

              // Section 8 + 9: staff leaderboard + revenue breakdown
              _Grid(columns: halfCols, ratioMinHeight: 240, children: [
                SectionCard(title: t.dashStaffLeaderboard, icon: Icons.emoji_events_outlined, child: _StaffLeaderboard(staff: data.staff, currency: cur)),
                SectionCard(title: t.dashRevenueBreakdown, icon: Icons.account_balance_wallet_outlined, child: _RevenueBreakdown(r: data.revenue)),
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
    final t = AppLocalizations.of(context);
    return SegmentedButton<DashboardRange>(
      showSelectedIcon: false,
      style: ButtonStyle(
        visualDensity: VisualDensity.compact,
        textStyle: WidgetStatePropertyAll(const TextStyle(fontSize: 12)),
      ),
      segments: [
        for (final r in DashboardRange.values)
          ButtonSegment(value: r, label: Text(_rangeLabel(t, r))),
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
    final l10n = AppLocalizations.of(context);
    if (tables.isEmpty) return _EmptyHint(l10n.dashNoActiveTables);
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
                    Text(l10n.commonTable(t.tableNumber),
                        style: const TextStyle(fontWeight: FontWeight.w700, color: Bo.text)),
                    Text('${t.capacity}p', style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                  ],
                ),
                const SizedBox(height: 6),
                ToneChip(tableStatusLabelL10n(l10n, t.status), tableStatusTone(t.status)),
                if (t.status != 'Available' && t.status != 'Reserved') ...[
                  const SizedBox(height: 8),
                  Text('${l10n.wtGuests(t.guestCount ?? 0)} · ${t.sessionMinutes ?? 0}m',
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
    final t = AppLocalizations.of(context);
    if (orders.isEmpty) return _EmptyHint(t.dashNoOrdersYet);
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: DataTable(
        columnSpacing: 28,
        headingRowHeight: 40,
        dataRowMinHeight: 40,
        dataRowMaxHeight: 48,
        columns: [
          DataColumn(label: Text(t.dashColOrder)),
          DataColumn(label: Text(t.dashColTable)),
          DataColumn(label: Text(t.dashColSource)),
          DataColumn(label: Text(t.dashColTime)),
          DataColumn(label: Text(t.dashColAmount)),
          DataColumn(label: Text(t.dashColStatus)),
        ],
        rows: [
          for (final o in orders)
            DataRow(cells: [
              DataCell(Text(o.orderNumber, style: const TextStyle(fontWeight: FontWeight.w600))),
              DataCell(Text(o.tableNumber ?? '—')),
              DataCell(Text('${orderTypeLabel(t, o.orderType)} · ${o.channel}')),
              DataCell(Text(shortDateTime(o.orderedAtUtc))),
              DataCell(Text(money(o.total, o.currency))),
              DataCell(ToneChip(orderStatusLabel(t, o.status), orderStatusTone(o.status))),
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
    final t = AppLocalizations.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Wrap(spacing: 8, runSpacing: 8, children: [
          MiniStat(t.dashStatAvgPrep, k.averagePrepMinutes.toStringAsFixed(1), tone: 'info'),
          MiniStat(t.dashStatCompleted, count(k.completedToday), tone: 'success'),
          MiniStat(t.dashStatWaitingOver10, count(k.ordersWaitingOver10Min), tone: k.ordersWaitingOver10Min > 0 ? 'danger' : 'neutral'),
        ]),
        const SizedBox(height: 12),
        if (k.longestWaitingOrderNumber != null)
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(color: Bo.warningSoft, borderRadius: BorderRadius.circular(Bo.radiusSm)),
            child: Text(t.dashLongestWaiting(k.longestWaitingOrderNumber!, k.longestWaitingMinutes ?? 0),
                style: const TextStyle(color: Bo.warning, fontWeight: FontWeight.w600, fontSize: 12)),
          ),
        const SizedBox(height: 12),
        Text(t.dashKitchenLoad, style: const TextStyle(color: Bo.textSubtle, fontSize: 12, fontWeight: FontWeight.w600)),
        const SizedBox(height: 6),
        Wrap(spacing: 8, runSpacing: 8, children: [
          MiniStat(t.dashStatPending, count(k.pendingCount), tone: 'neutral'),
          MiniStat(t.dashStatPreparing, count(k.preparingCount), tone: 'warning'),
          MiniStat(t.dashStatReady, count(k.readyCount), tone: 'primary'),
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
    final l10n = AppLocalizations.of(context);
    if (requests.isEmpty) return _EmptyHint(l10n.dashNoPendingRequests);
    return Column(
      children: [
        for (final r in requests)
          ListTile(
            dense: true,
            contentPadding: EdgeInsets.zero,
            leading: const Icon(Icons.notifications_none, size: 20, color: Bo.textMuted),
            title: Text('${l10n.commonTable(r.tableNumber)} · ${requestTypeLabel(l10n, r.type)}',
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
    final t = AppLocalizations.of(context);
    return _Grid(columns: columns, ratioMinHeight: 160, children: [
      _AlertList(
        title: t.dashLowStock,
        tone: 'warning',
        rows: [for (final i in inv.lowStock) '${i.name} — ${i.qtyOnHand} ${i.unitCode}'],
      ),
      _AlertList(
        title: t.dashOutOfStock,
        tone: 'danger',
        rows: [for (final i in inv.outOfStock) i.name],
      ),
      _AlertList(
        title: t.dashTodaysConsumption,
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
    final t = AppLocalizations.of(context);
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
            Text(t.dashNone, style: const TextStyle(color: Bo.textSubtle, fontSize: 12))
          else
            for (final r in rows.take(6))
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 2),
                child: Text(r, style: const TextStyle(fontSize: 12, color: Bo.textMuted), overflow: TextOverflow.ellipsis),
              ),
          if (rows.length > 6)
            Text(t.dashMore(rows.length - 6), style: const TextStyle(fontSize: 11, color: Bo.textSubtle)),
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
    final t = AppLocalizations.of(context);
    if (staff.isEmpty) return _EmptyHint(t.dashNoStaffActivity);
    return Column(
      children: [
        for (final s in staff.take(8))
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 5),
            child: Row(
              children: [
                Expanded(flex: 4, child: Text(s.waiterName, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 13, color: Bo.text))),
                Expanded(flex: 2, child: Text('${s.ordersProcessed} ${t.dashStatOrd}', style: const TextStyle(fontSize: 12, color: Bo.textSubtle))),
                Expanded(flex: 2, child: Text('${s.tablesAssigned} ${t.dashStatTbl}', style: const TextStyle(fontSize: 12, color: Bo.textSubtle))),
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
    final t = AppLocalizations.of(context);
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
        row(t.dashRevDineIn, r.dineInRevenue),
        row(t.dashRevTakeaway, r.takeawayRevenue),
        row(t.dashRevDelivery, r.deliveryRevenue),
        row(t.dashRevQrOrdering, r.qrOrderingRevenue),
        const Divider(),
        row(t.dashRevDiscount, r.discountAmount),
        row(t.dashRevTaxCollected, r.taxCollected),
        row(t.dashRevServiceCharge, r.serviceChargeCollected),
        const Divider(),
        row(t.dashRevGrandTotal, r.grandTotal, bold: true),
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
            FilledButton.icon(onPressed: onRetry, icon: const Icon(Icons.refresh), label: Text(AppLocalizations.of(context).actionRetry)),
          ],
        ),
      ),
    );
  }
}
