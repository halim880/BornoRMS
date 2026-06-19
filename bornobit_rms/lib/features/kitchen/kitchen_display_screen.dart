import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/i18n/labels.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../../l10n/app_localizations.dart';
import '../dashboard/widgets.dart';
import 'kitchen_api.dart';
import 'kitchen_models.dart';
import 'kitchen_providers.dart';

/// Route for the Kitchen Display screen (mirrors the Blazor /operations/kitchen-display).
const kitchenDisplayRoute = '/operations/kitchen-display';

/// Live Kitchen Display (KDS): orders grouped into Pending / Preparing / Ready
/// columns, KPI metrics on top, and one-tap accept/advance ("bump") actions.
/// Mirrors the Blazor KitchenDisplay.razor, polled (no SignalR).
class KitchenDisplayScreen extends ConsumerStatefulWidget {
  const KitchenDisplayScreen({super.key});

  @override
  ConsumerState<KitchenDisplayScreen> createState() => _KitchenDisplayScreenState();
}

class _KitchenDisplayScreenState extends ConsumerState<KitchenDisplayScreen> {
  bool _busy = false;
  final _tableController = TextEditingController();
  final _searchController = TextEditingController();

  // 1s ticker so the per-card elapsed time / colour recomputes between polls.
  DateTime _nowUtc = DateTime.now().toUtc();
  late final Stream<void> _tick;

  @override
  void initState() {
    super.initState();
    _tableController.text = ref.read(kitchenTableFilterProvider);
    _searchController.text = ref.read(kitchenSearchFilterProvider);
    _tick = Stream<void>.periodic(const Duration(seconds: 1));
  }

  @override
  void dispose() {
    _tableController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _reload() => ref.read(kitchenBoardProvider.notifier).refresh();

  Future<void> _act(Future<void> Function() action, String okMessage) async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      await action();
      await _reload();
      if (mounted) AppToast.show(context, okMessage);
    } catch (e) {
      if (mounted) {
        AppToast.show(context, _errorText(e), type: ToastType.error);
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  String _errorText(Object e) {
    final s = e.toString();
    return s.startsWith('Exception: ') ? s.substring(11) : s;
  }

  // ---------- filter mutators (re-fetch board) ----------
  void _setStation(String? id) {
    ref.read(kitchenStationFilterProvider.notifier).state = id;
    _reload();
  }

  void _setType(String? t) {
    ref.read(kitchenTypeFilterProvider.notifier).state = t;
    _reload();
  }

  void _setTable(String v) {
    ref.read(kitchenTableFilterProvider.notifier).state = v.trim();
    _reload();
  }

  void _setSearch(String v) {
    ref.read(kitchenSearchFilterProvider.notifier).state = v.trim();
    _reload();
  }

  void _clearFilters() {
    _tableController.clear();
    _searchController.clear();
    ref.read(kitchenStationFilterProvider.notifier).state = null;
    ref.read(kitchenTypeFilterProvider.notifier).state = null;
    ref.read(kitchenTableFilterProvider.notifier).state = '';
    ref.read(kitchenSearchFilterProvider.notifier).state = '';
    _reload();
  }

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(kitchenBoardProvider);
    final stationId = ref.watch(kitchenStationFilterProvider);
    final typeFilter = ref.watch(kitchenTypeFilterProvider);

    final t = AppLocalizations.of(context);
    // Drive the 1s clock.
    return StreamBuilder<void>(
      stream: _tick,
      builder: (context, _) {
        _nowUtc = DateTime.now().toUtc();
        return Column(
          children: [
            PageHeader(
              title: t.kdsTitle,
              subtitle: t.kdsSubtitle,
              actions: [
                IconButton(
                  tooltip: t.actionRefresh,
                  onPressed: _busy ? null : _reload,
                  icon: const Icon(Icons.refresh),
                ),
              ],
            ),
            Expanded(
              child: AsyncStateView<KitchenConsole>(
                isLoading: async.isLoading,
                error: async.hasError ? async.error : null,
                value: async.valueOrNull,
                onRetry: _reload,
                data: (console) => _body(console, stationId, typeFilter),
              ),
            ),
          ],
        );
      },
    );
  }

  Widget _body(KitchenConsole console, String? stationId, String? typeFilter) {
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _metrics(console.metrics),
          const SizedBox(height: 12),
          _stationTabs(console.stations, stationId),
          const SizedBox(height: 12),
          _filters(typeFilter),
          const SizedBox(height: 12),
          _board(console.board, showStationTag: stationId == null),
        ],
      ),
    );
  }

  // ---------- metrics ----------
  Widget _metrics(KitchenMetrics m) {
    final t = AppLocalizations.of(context);
    final longest = m.longestWaitingMinutes;
    return KpiGrid(
      minTileWidth: 180,
      children: [
        KpiCard(
          label: t.kdsKpiPending,
          value: count(m.pendingCount),
          icon: Icons.hourglass_empty,
          tint: Bo.bgSoft,
        ),
        KpiCard(
          label: t.kdsKpiPreparing,
          value: count(m.preparingCount),
          icon: Icons.outdoor_grill,
          tint: Bo.warningSoft,
        ),
        KpiCard(
          label: t.kdsKpiReady,
          value: count(m.readyCount),
          icon: Icons.room_service,
          tint: Bo.primaryTint,
        ),
        KpiCard(
          label: t.kdsKpiAvgPrep,
          value: '${m.averagePrepMinutes.toStringAsFixed(1)} m',
          icon: Icons.timer,
          tint: Bo.infoSoft,
        ),
        KpiCard(
          label: '${t.kdsKpiLongestWait}${m.longestWaitingOrderNumber != null ? ' · ${m.longestWaitingOrderNumber}' : ''}',
          value: longest == null ? '–' : '$longest m',
          icon: Icons.warning_amber,
          tint: (longest != null && longest >= 20) ? Bo.dangerSoft : Bo.bgSoft,
        ),
        KpiCard(
          label: t.kdsKpiDoneToday,
          value: count(m.completedToday),
          icon: Icons.check_circle,
          tint: Bo.successSoft,
        ),
      ],
    );
  }

  // ---------- station tabs ----------
  Widget _stationTabs(List<KitchenStation> stations, String? selected) {
    Widget tab(String label, String? id) {
      final on = selected == id;
      return Padding(
        padding: const EdgeInsets.only(right: 8),
        child: ChoiceChip(
          label: Text(label),
          selected: on,
          onSelected: (_) => _setStation(id),
        ),
      );
    }

    final t = AppLocalizations.of(context);
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          tab(t.kdsAllStations, null),
          for (final s in stations) tab(s.name, s.id),
        ],
      ),
    );
  }

  // ---------- filters ----------
  Widget _filters(String? typeFilter) {
    final t = AppLocalizations.of(context);
    const types = ['DineIn', 'Takeaway', 'Delivery', 'Collection', 'Waiting'];
    return Wrap(
      spacing: 12,
      runSpacing: 8,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        SizedBox(
          width: 180,
          child: DropdownButtonFormField<String?>(
            initialValue: typeFilter,
            isExpanded: true,
            decoration: InputDecoration(labelText: t.kdsFilterType, isDense: true),
            items: [
              DropdownMenuItem<String?>(value: null, child: Text(t.commonAllTypes)),
              for (final ty in types)
                DropdownMenuItem<String?>(value: ty, child: Text(orderTypeLabel(t, ty))),
            ],
            onChanged: _setType,
          ),
        ),
        SizedBox(
          width: 140,
          child: TextField(
            controller: _tableController,
            decoration: InputDecoration(labelText: t.kdsFilterTable, isDense: true),
            onSubmitted: _setTable,
          ),
        ),
        SizedBox(
          width: 180,
          child: TextField(
            controller: _searchController,
            decoration: InputDecoration(labelText: t.kdsFilterSearch, isDense: true),
            onSubmitted: _setSearch,
          ),
        ),
        OutlinedButton(onPressed: _clearFilters, child: Text(t.actionClear)),
      ],
    );
  }

  // ---------- board ----------
  Widget _board(KitchenBoard board, {required bool showStationTag}) {
    final t = AppLocalizations.of(context);
    return LayoutBuilder(
      builder: (context, c) {
        final cols = [
          _column(t.kdsColPending, board.pending, 'neutral', showStationTag),
          _column(t.kdsColPreparing, board.preparing, 'warning', showStationTag),
          _column(t.kdsColReady, board.ready, 'primary', showStationTag),
        ];
        // Wide layout: three side-by-side columns; narrow: stacked.
        if (c.maxWidth >= 900) {
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (var i = 0; i < cols.length; i++) ...[
                if (i > 0) const SizedBox(width: 12),
                Expanded(child: cols[i]),
              ],
            ],
          );
        }
        return Column(
          children: [
            for (var i = 0; i < cols.length; i++) ...[
              if (i > 0) const SizedBox(height: 12),
              cols[i],
            ],
          ],
        );
      },
    );
  }

  Widget _column(String title, List<KitchenOrderCard> cards, String tone, bool showStationTag) {
    final t = AppLocalizations.of(context);
    return SectionCard(
      title: title,
      trailing: ToneChip(count(cards.length), tone),
      child: cards.isEmpty
          ? Padding(
              padding: const EdgeInsets.symmetric(vertical: 24),
              child: EmptyState(message: t.kdsNoOrders, icon: Icons.restaurant_menu),
            )
          : Column(
              children: [
                for (final card in cards)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: _OrderCard(
                      card: card,
                      nowUtc: _nowUtc,
                      showStationTag: showStationTag,
                      busy: _busy,
                      onAccept: () => _act(
                        () => ref.read(staffApiProvider).kitchenAccept(card.id),
                        t.kdsToastAccepted,
                      ),
                      onAdvance: () => _act(
                        () => ref.read(staffApiProvider).kitchenAdvance(card.id),
                        _advanceMessage(t, card.status),
                      ),
                      onTogglePriority: () => _act(
                        () => ref.read(staffApiProvider).kitchenTogglePriority(card.id, !card.isPriority),
                        card.isPriority ? t.kdsToastPriorityCleared : t.kdsToastPriorityMarked,
                      ),
                      onEditNotes: () => _editNotes(card),
                    ),
                  ),
              ],
            ),
    );
  }

  String _advanceMessage(AppLocalizations t, String status) => switch (status) {
        'Confirmed' => t.kdsToastCooking,
        'Preparing' => t.kdsToastReady,
        'Ready' => t.kdsToastServed,
        _ => t.kdsToastAdvanced,
      };

  Future<void> _editNotes(KitchenOrderCard card) async {
    final t = AppLocalizations.of(context);
    final controller = TextEditingController(text: card.kitchenNotes ?? '');
    final saved = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(t.kdsNotesTitle(card.orderNumber)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(t.kdsNotesInternal,
                style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
            const SizedBox(height: 12),
            TextField(
              controller: controller,
              maxLines: 4,
              autofocus: true,
              decoration: InputDecoration(
                hintText: t.kdsNotesHint,
              ),
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.actionCancel)),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: Text(t.actionSave)),
        ],
      ),
    );
    if (saved == true) {
      final notes = controller.text.trim();
      await _act(
        () => ref.read(staffApiProvider).kitchenSaveNotes(card.id, notes.isEmpty ? null : notes),
        t.kdsToastNotesSaved,
      );
    }
    controller.dispose();
  }
}

/// A single order ticket card with status-appropriate action buttons.
class _OrderCard extends StatelessWidget {
  final KitchenOrderCard card;
  final DateTime nowUtc;
  final bool showStationTag;
  final bool busy;
  final VoidCallback onAccept;
  final VoidCallback onAdvance;
  final VoidCallback onTogglePriority;
  final VoidCallback onEditNotes;

  const _OrderCard({
    required this.card,
    required this.nowUtc,
    required this.showStationTag,
    required this.busy,
    required this.onAccept,
    required this.onAdvance,
    required this.onTogglePriority,
    required this.onEditNotes,
  });

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final elapsed = card.elapsedMinutes(nowUtc);
    final elapsedColor = elapsed >= 20
        ? Bo.danger
        : elapsed >= 10
            ? Bo.warning
            : Bo.textSubtle;

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Bo.bgSoft,
        borderRadius: BorderRadius.circular(Bo.radiusMd),
        border: Border.all(color: card.isPriority ? Bo.danger : Bo.border, width: card.isPriority ? 1.5 : 1),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              if (card.isPriority) ...[
                const Icon(Icons.priority_high, size: 16, color: Bo.danger),
                const SizedBox(width: 2),
              ],
              Expanded(
                child: Text(card.orderNumber,
                    style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14, color: Bo.text)),
              ),
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.schedule, size: 13),
                  const SizedBox(width: 3),
                  Text('$elapsed m', style: TextStyle(color: elapsedColor, fontWeight: FontWeight.w700, fontSize: 12)),
                ],
              ),
            ],
          ),
          const SizedBox(height: 6),
          Wrap(
            spacing: 6,
            runSpacing: 4,
            children: [
              ToneChip(orderStatusLabel(t, card.status), orderStatusTone(card.status)),
              ToneChip(orderTypeLabel(t, card.orderType), 'info'),
              if (card.tableNumber != null && card.tableNumber!.isNotEmpty)
                ToneChip(t.commonTable(card.tableNumber!), 'neutral'),
            ],
          ),
          if (card.customerName != null && card.customerName!.isNotEmpty) ...[
            const SizedBox(height: 6),
            Text(card.customerName!, style: const TextStyle(color: Bo.textMuted, fontSize: 12)),
          ],
          const SizedBox(height: 4),
          Text(card.source, style: const TextStyle(color: Bo.textSubtle, fontSize: 11)),
          const Divider(height: 14),
          for (final item in card.items)
            Padding(
              padding: const EdgeInsets.only(bottom: 4),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${item.quantity}×',
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13, color: Bo.primaryEmphasis)),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(item.name, style: const TextStyle(fontSize: 13, color: Bo.text)),
                        if (item.notes != null && item.notes!.isNotEmpty)
                          Text(item.notes!, style: const TextStyle(fontSize: 11, color: Bo.warning)),
                        if (showStationTag && item.stationName != null && item.stationName!.isNotEmpty)
                          Text(item.stationName!, style: const TextStyle(fontSize: 10, color: Bo.textSubtle)),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          if (card.kitchenNotes != null && card.kitchenNotes!.isNotEmpty) ...[
            const SizedBox(height: 4),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(color: Bo.warningSoft, borderRadius: BorderRadius.circular(Bo.radiusSm)),
              child: Text(t.commonNote(card.kitchenNotes!), style: const TextStyle(fontSize: 11, color: Bo.warning)),
            ),
          ],
          const SizedBox(height: 10),
          _actions(t),
        ],
      ),
    );
  }

  Widget _actions(AppLocalizations t) {
    final children = <Widget>[];

    // Placed/Confirmed → Accept (only Placed is acceptable; Confirmed can advance to cooking).
    if (card.status == 'Placed') {
      children.add(Expanded(
        child: FilledButton.icon(
          onPressed: busy ? null : onAccept,
          icon: const Icon(Icons.check, size: 16),
          label: Text(t.kdsAccept),
        ),
      ));
    } else {
      final (label, icon) = switch (card.status) {
        'Confirmed' => (t.kdsStartCooking, Icons.outdoor_grill),
        'Preparing' => (t.kdsMarkReady, Icons.room_service),
        'Ready' => (t.kdsServe, Icons.done_all),
        _ => (t.kdsAdvance, Icons.arrow_forward),
      };
      children.add(Expanded(
        child: FilledButton.icon(
          onPressed: busy ? null : onAdvance,
          icon: Icon(icon, size: 16),
          label: Text(label),
        ),
      ));
    }

    children.add(const SizedBox(width: 6));
    children.add(IconButton(
      tooltip: card.isPriority ? t.kdsClearPriority : t.kdsMarkPriority,
      onPressed: busy ? null : onTogglePriority,
      icon: Icon(card.isPriority ? Icons.flag : Icons.outlined_flag,
          color: card.isPriority ? Bo.danger : Bo.textSubtle),
    ));
    children.add(IconButton(
      tooltip: t.kdsKitchenNotes,
      onPressed: busy ? null : onEditNotes,
      icon: const Icon(Icons.sticky_note_2_outlined, color: Bo.textSubtle),
    ));

    return Row(children: children);
  }
}
