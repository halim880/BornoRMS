import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/i18n/labels.dart';
import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../../l10n/app_localizations.dart';
import '../dashboard/widgets.dart';
import 'waiter_api.dart';
import 'waiter_models.dart';
import 'waiter_providers.dart';

/// Waiter console — floor map, my sessions, ready-to-serve, and customer
/// requests. Mirrors the Blazor WaiterOrders.razor, polled (no SignalR).
class WaiterScreen extends ConsumerStatefulWidget {
  const WaiterScreen({super.key});

  @override
  ConsumerState<WaiterScreen> createState() => _WaiterScreenState();
}

class _WaiterScreenState extends ConsumerState<WaiterScreen> with SingleTickerProviderStateMixin {
  late final TabController _tabs = TabController(length: 4, vsync: this);

  @override
  void dispose() {
    _tabs.dispose();
    super.dispose();
  }

  Future<void> _run(Future<void> Function() action, String okMessage) async {
    try {
      await action();
      if (!mounted) return;
      await ref.read(waiterConsoleProvider.notifier).refresh();
      if (mounted) AppToast.show(context, okMessage);
    } catch (e) {
      if (mounted) AppToast.show(context, '$e', type: ToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final async = ref.watch(waiterConsoleProvider);
    return AsyncStateView<WaiterConsoleData>(
      isLoading: async.isLoading,
      error: async.hasError ? async.error : null,
      value: async.valueOrNull,
      onRetry: () => ref.invalidate(waiterConsoleProvider),
      data: (d) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
            child: _KpiStrip(d: d.dashboard),
          ),
          TabBar(
            controller: _tabs,
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            labelColor: Bo.primary,
            unselectedLabelColor: Bo.textMuted,
            indicatorColor: Bo.primary,
            tabs: [
              Tab(text: t.wtTabFloor(d.floor.length)),
              Tab(text: t.wtTabSessions(d.mySessions.length)),
              Tab(text: t.wtTabReady(d.ready.length)),
              Tab(text: t.wtTabRequests(d.requests.length)),
            ],
          ),
          const Divider(height: 1),
          Expanded(
            child: TabBarView(
              controller: _tabs,
              children: [
                _FloorTab(floor: d.floor, onRequestPayment: _requestPayment, onBill: _showBill),
                _SessionsTab(
                  sessions: d.mySessions,
                  onRequestPayment: _requestPayment,
                  onClose: _closeSession,
                  onBill: _showBill,
                ),
                _ReadyTab(ready: d.ready, onServe: _markServed),
                _RequestsTab(requests: d.requests, onResolve: _resolveRequest),
              ],
            ),
          ),
        ],
      ),
    );
  }

  void _requestPayment(String sessionId) => _run(
      () => ref.read(staffApiProvider).waiterRequestPayment(sessionId),
      AppLocalizations.of(context).wtToastCashierNotified);

  void _closeSession(String sessionId) => _run(
      () => ref.read(staffApiProvider).waiterCloseSession(sessionId),
      AppLocalizations.of(context).wtToastSessionClosed);

  void _markServed(String orderId) => _run(
      () => ref.read(staffApiProvider).waiterChangeStatus(orderId, 'Served'),
      AppLocalizations.of(context).wtToastMarkedServed);

  void _resolveRequest(String requestId) => _run(
      () => ref.read(staffApiProvider).waiterResolveRequest(requestId),
      AppLocalizations.of(context).wtToastRequestResolved);

  void _showBill(String sessionId) {
    showDialog(context: context, builder: (_) => _SessionBillDialog(sessionId: sessionId));
  }
}

class _KpiStrip extends StatelessWidget {
  final WaiterDashboard d;
  const _KpiStrip({required this.d});

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return KpiGrid(
      minTileWidth: 180,
      children: [
        KpiCard(
          label: t.wtKpiMyTables,
          value: '${d.myTables}',
          icon: Icons.table_restaurant_outlined,
          tint: Bo.primarySoft,
          stats: [MiniStat(t.wtStatActive, '${d.myActiveSessions}', tone: 'primary')],
        ),
        KpiCard(
          label: t.wtKpiAvailable,
          value: '${d.availableTables}',
          icon: Icons.event_available_outlined,
          tint: Bo.primarySoft,
          stats: [MiniStat(t.wtStatOccupied, '${d.occupiedTables}', tone: 'info')],
        ),
        KpiCard(
          label: t.wtKpiReadyToServe,
          value: '${d.readyToServeOrders}',
          icon: Icons.room_service_outlined,
          tint: Bo.primarySoft,
        ),
        KpiCard(
          label: t.wtKpiPendingRequests,
          value: '${d.pendingRequests}',
          icon: Icons.notifications_active_outlined,
          tint: Bo.primarySoft,
          stats: [MiniStat(t.wtStatBills, '${d.billsWaiting}', tone: 'warning')],
        ),
        KpiCard(
          label: t.wtKpiMyRevenue,
          value: money(d.myRevenueServedToday, d.currency),
          icon: Icons.payments_outlined,
          tint: Bo.primarySoft,
        ),
      ],
    );
  }
}

// ---------------- Floor ----------------
class _FloorTab extends StatelessWidget {
  final List<TableOverviewRow> floor;
  final void Function(String sessionId) onRequestPayment;
  final void Function(String sessionId) onBill;
  const _FloorTab({required this.floor, required this.onRequestPayment, required this.onBill});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    if (floor.isEmpty) return EmptyState(message: l10n.wtNoTables, icon: Icons.table_bar_outlined);
    return GridView.builder(
      padding: const EdgeInsets.all(16),
      gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
        maxCrossAxisExtent: 260,
        mainAxisExtent: 158,
        crossAxisSpacing: 12,
        mainAxisSpacing: 12,
      ),
      itemCount: floor.length,
      itemBuilder: (_, i) {
        final t = floor[i];
        final seats = '${l10n.wtSeats(t.capacity)}${t.guestCount != null ? ' · ${l10n.wtGuests(t.guestCount!)}' : ''}';
        return Card(
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(l10n.commonTable(t.tableNumber),
                        style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 15)),
                    const Spacer(),
                    ToneChip(tableStatusLabelL10n(l10n, t.status), tableStatusTone(t.status)),
                  ],
                ),
                const SizedBox(height: 6),
                Text(seats, style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                if (t.waiterName != null)
                  Text(t.waiterName!, style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                const Spacer(),
                if (t.sessionId != null) ...[
                  Text(money(t.currentBill, t.currency),
                      style: const TextStyle(fontWeight: FontWeight.w700)),
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => onBill(t.sessionId!),
                          child: Text(l10n.wtBill),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: FilledButton(
                          onPressed: () => onRequestPayment(t.sessionId!),
                          child: Text(l10n.wtPay),
                        ),
                      ),
                    ],
                  ),
                ] else
                  Text(l10n.wtFree, style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
              ],
            ),
          ),
        );
      },
    );
  }
}

// ---------------- Sessions ----------------
class _SessionsTab extends StatelessWidget {
  final List<SessionRow> sessions;
  final void Function(String sessionId) onRequestPayment;
  final void Function(String sessionId) onClose;
  final void Function(String sessionId) onBill;
  const _SessionsTab({
    required this.sessions,
    required this.onRequestPayment,
    required this.onClose,
    required this.onBill,
  });

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    if (sessions.isEmpty) return EmptyState(message: l10n.wtNoSessions, icon: Icons.receipt_long_outlined);
    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: sessions.length,
      separatorBuilder: (_, __) => const SizedBox(height: 10),
      itemBuilder: (_, i) {
        final s = sessions[i];
        final meta = '${s.sessionNumber} · ${l10n.wtGuests(s.guestCount)} · ${l10n.wtOrders(s.orderCount)} · ${s.sessionMinutes}m';
        return Card(
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(children: [
                        Text(l10n.commonTable(s.tableNumber), style: const TextStyle(fontWeight: FontWeight.w800)),
                        const SizedBox(width: 8),
                        ToneChip(s.status, s.status == 'Billing' ? 'warning' : 'primary'),
                      ]),
                      const SizedBox(height: 4),
                      Text(meta, style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                    ],
                  ),
                ),
                Text(money(s.runningBill, s.currency), style: const TextStyle(fontWeight: FontWeight.w800)),
                const SizedBox(width: 12),
                OutlinedButton(onPressed: () => onBill(s.id), child: Text(l10n.wtBill)),
                const SizedBox(width: 8),
                OutlinedButton(onPressed: () => onRequestPayment(s.id), child: Text(l10n.wtPay)),
                const SizedBox(width: 8),
                TextButton(onPressed: () => onClose(s.id), child: Text(l10n.wtClose)),
              ],
            ),
          ),
        );
      },
    );
  }
}

// ---------------- Ready ----------------
class _ReadyTab extends StatelessWidget {
  final List<ReadyToServeRow> ready;
  final void Function(String orderId) onServe;
  const _ReadyTab({required this.ready, required this.onServe});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    if (ready.isEmpty) return EmptyState(message: l10n.wtNothingReady, icon: Icons.room_service_outlined);
    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: ready.length,
      separatorBuilder: (_, __) => const SizedBox(height: 10),
      itemBuilder: (_, i) {
        final r = ready[i];
        return Card(
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(r.orderNumber, style: const TextStyle(fontWeight: FontWeight.w800)),
                    const SizedBox(width: 8),
                    if (r.tableNumber != null) Text(l10n.commonTable(r.tableNumber!), style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                    const Spacer(),
                    ToneChip('${r.waitingMinutes}m', r.waitingMinutes > 10 ? 'danger' : 'warning'),
                    const SizedBox(width: 8),
                    FilledButton(onPressed: () => onServe(r.orderId), child: Text(l10n.wtServe)),
                  ],
                ),
                const SizedBox(height: 6),
                for (final it in r.items)
                  Text('${it.quantity}× ${it.name}${it.stationName != null ? '  ·  ${it.stationName}' : ''}',
                      style: const TextStyle(fontSize: 13)),
              ],
            ),
          ),
        );
      },
    );
  }
}

// ---------------- Requests ----------------
class _RequestsTab extends StatelessWidget {
  final List<CustomerRequestRow> requests;
  final void Function(String requestId) onResolve;
  const _RequestsTab({required this.requests, required this.onResolve});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    if (requests.isEmpty) return EmptyState(message: l10n.wtNoRequests, icon: Icons.notifications_none);
    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: requests.length,
      separatorBuilder: (_, __) => const SizedBox(height: 10),
      itemBuilder: (_, i) {
        final r = requests[i];
        return Card(
          child: ListTile(
            leading: const Icon(Icons.notifications_active_outlined, color: Bo.warning),
            title: Text(requestTypeLabel(l10n, r.type), style: const TextStyle(fontWeight: FontWeight.w700)),
            subtitle: Text('${l10n.commonTable(r.tableNumber)} · ${r.waitingMinutes}m${r.note != null ? ' · ${r.note}' : ''}'),
            trailing: FilledButton(onPressed: () => onResolve(r.id), child: Text(l10n.wtResolve)),
          ),
        );
      },
    );
  }
}

// ---------------- Bill dialog ----------------
class _SessionBillDialog extends ConsumerWidget {
  final String sessionId;
  const _SessionBillDialog({required this.sessionId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(sessionBillProvider(sessionId));
    return Dialog(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 560, maxHeight: 680),
        child: async.when(
          loading: () => const SizedBox(height: 240, child: Center(child: CircularProgressIndicator())),
          error: (e, _) => SizedBox(height: 240, child: ErrorRetry(message: e.toString())),
          data: (b) => _BillBody(b: b),
        ),
      ),
    );
  }
}

class _BillBody extends StatelessWidget {
  final SessionBill b;
  const _BillBody({required this.b});

  Widget _row(String l, double v, {bool bold = false}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
          Text(l, style: TextStyle(fontSize: 13, color: bold ? Bo.text : Bo.textMuted, fontWeight: bold ? FontWeight.w800 : FontWeight.w400)),
          Text(money(v, b.currency), style: TextStyle(fontSize: 13, fontWeight: bold ? FontWeight.w800 : FontWeight.w600)),
        ]),
      );

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          padding: const EdgeInsets.all(16),
          decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: Bo.border))),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(l10n.commonTable(b.tableNumber), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                    Text('${b.sessionNumber} · ${l10n.wtGuests(b.guestCount)}', style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                  ],
                ),
              ),
              IconButton(onPressed: () => Navigator.of(context).pop(), icon: const Icon(Icons.close)),
            ],
          ),
        ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              for (final o in b.orders) ...[
                Row(children: [
                  Text(o.orderNumber, style: const TextStyle(fontWeight: FontWeight.w700)),
                  const SizedBox(width: 8),
                  ToneChip(orderStatusLabel(l10n, o.status), orderStatusTone(o.status)),
                  const Spacer(),
                  if (o.isPaid) ToneChip(l10n.billPaid, 'success'),
                ]),
                const SizedBox(height: 4),
                for (final l in o.lines)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 2),
                    child: Row(children: [
                      Text('${l.quantity}×', style: const TextStyle(fontWeight: FontWeight.w700, color: Bo.textMuted)),
                      const SizedBox(width: 8),
                      Expanded(child: Text(l.name, overflow: TextOverflow.ellipsis)),
                      Text(money(l.lineTotal, b.currency), style: const TextStyle(fontWeight: FontWeight.w600)),
                    ]),
                  ),
                const Divider(),
              ],
              _row(l10n.billSubtotalLabel, b.subtotal),
              if (b.discountAmount != 0) _row(l10n.billDiscount, -b.discountAmount),
              if (b.taxAmount != 0) _row(l10n.billTax, b.taxAmount),
              if (b.serviceChargeAmount != 0) _row(l10n.billServiceCharge, b.serviceChargeAmount),
              if (b.roundingAdjustment != 0) _row(l10n.billRounding, b.roundingAdjustment),
              _row(l10n.billGrandTotal, b.grandTotal, bold: true),
              if (b.paidAmount != 0) _row(l10n.billPaid, b.paidAmount),
              if (b.balanceDue != 0) _row(l10n.billBalanceDue, b.balanceDue, bold: true),
            ],
          ),
        ),
      ],
    );
  }
}
