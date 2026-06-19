import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_api.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const dayEndRoute = '/accounts/reports/day-end';

/// Accounts → Reports → Day End. Day-close summary for one business day. Mirrors
/// the Blazor DayEnd.razor page. Read-only.
class DayEndScreen extends ConsumerWidget {
  const DayEndScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(dayEndProvider);
    final date = ref.watch(dayEndDateProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Day End',
          subtitle: 'Reconcile one business day: sales, collection, drawers, expenses.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(dayEndProvider))],
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
          child: Row(
            children: [
              OutlinedButton.icon(
                icon: const Icon(Icons.calendar_today, size: 16),
                label: Text(shortDate(date)),
                onPressed: () async {
                  final picked = await showDatePicker(
                    context: context,
                    initialDate: date,
                    firstDate: DateTime(2000),
                    lastDate: DateTime.now(),
                  );
                  if (picked != null) {
                    ref.read(dayEndDateProvider.notifier).state =
                        DateTime(picked.year, picked.month, picked.day);
                  }
                },
              ),
              const Spacer(),
              _PostToGlButton(date: date),
            ],
          ),
        ),
        Expanded(
          child: AsyncStateView<DayEndReport>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(dayEndProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(DayEndReport r) {
    final cur = r.currency;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        KpiGrid(children: [
          KpiCard(label: 'Orders', value: count(r.orderCount), icon: Icons.receipt_long, tint: Bo.primaryTint),
          KpiCard(label: 'Sales Total', value: money(r.salesTotal, cur), icon: Icons.payments, tint: Bo.successSoft),
          KpiCard(label: 'Collected', value: money(r.totalCollected, cur), icon: Icons.account_balance_wallet, tint: Bo.infoSoft),
          KpiCard(label: 'Expenses', value: money(r.totalExpenses, cur), icon: Icons.north_east, tint: Bo.dangerSoft),
          KpiCard(label: 'Drawer Variance', value: money(r.drawerVariance, cur), icon: Icons.balance, tint: Bo.warningSoft),
          KpiCard(label: 'Unaccounted', value: money(r.unaccountedAmount, cur), icon: Icons.help_outline, tint: Bo.warningSoft,
              stats: [MiniStat('orders', count(r.unaccountedOrders), tone: 'warning')]),
        ]),
        const SizedBox(height: 16),
        SectionCard(
          title: 'Collection by Method',
          child: r.byMethod.isEmpty
              ? const Text('No payments captured.', style: TextStyle(color: Bo.textSubtle))
              : Column(children: [
                  for (final m in r.byMethod)
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 6),
                      child: Row(children: [
                        Expanded(child: Text(m.method)),
                        Text('${m.count}×  ', style: const TextStyle(color: Bo.textSubtle)),
                        Text(money(m.amount, cur), style: const TextStyle(fontWeight: FontWeight.w600)),
                      ]),
                    ),
                ]),
        ),
        const SizedBox(height: 12),
        SectionCard(
          title: 'Drawers',
          child: r.drawers.isEmpty
              ? const Text('No drawers opened.', style: TextStyle(color: Bo.textSubtle))
              : Column(children: [
                  for (final d in r.drawers)
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 6),
                      child: Row(children: [
                        Expanded(
                            child: Text('${d.drawerNumber} · ${d.cashierName}',
                                overflow: TextOverflow.ellipsis)),
                        ToneChip(d.status, accountsStatusTone(d.status)),
                        const SizedBox(width: 8),
                        Text(money(d.variance, cur),
                            style: TextStyle(
                                fontWeight: FontWeight.w600,
                                color: d.variance == 0 ? Bo.success : Bo.danger)),
                      ]),
                    ),
                ]),
        ),
        const SizedBox(height: 12),
        SectionCard(
          title: 'Expenses',
          child: r.expenses.isEmpty
              ? const Text('No expenses booked.', style: TextStyle(color: Bo.textSubtle))
              : Column(children: [
                  for (final e in r.expenses)
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 6),
                      child: Row(children: [
                        Expanded(child: Text(e.categoryName)),
                        Text(money(e.amount, cur), style: const TextStyle(fontWeight: FontWeight.w600)),
                      ]),
                    ),
                ]),
        ),
      ],
    );
  }
}

/// Posts the selected day's un-accounted takings to the GL on demand. The background service does this
/// automatically at day-end; this is the manual override + visible "books are behind" remedy.
class _PostToGlButton extends ConsumerStatefulWidget {
  final DateTime date;
  const _PostToGlButton({required this.date});

  @override
  ConsumerState<_PostToGlButton> createState() => _PostToGlButtonState();
}

class _PostToGlButtonState extends ConsumerState<_PostToGlButton> {
  bool _busy = false;

  Future<void> _run() async {
    setState(() => _busy = true);
    try {
      final result = await ref.read(staffApiProvider).cashCounterImport(date: widget.date);
      ref.invalidate(dayEndProvider);
      if (!mounted) return;
      final msg = result.count == 0
          ? 'Nothing to post — the books are already up to date.'
          : 'Posted ${result.count} order(s) totalling ${money(result.total, 'Tk')} to the GL.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text('Import failed: $e'), backgroundColor: Bo.danger));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FilledButton.icon(
      onPressed: _busy ? null : _run,
      icon: _busy
          ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
          : const Icon(Icons.account_balance, size: 16),
      label: const Text('Post to GL'),
    );
  }
}
