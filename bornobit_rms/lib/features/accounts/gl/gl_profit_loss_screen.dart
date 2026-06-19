import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/app_page.dart';
import '../../dashboard/widgets.dart';
import '../accounts_models.dart';
import '../accounts_providers.dart';
import '../widgets.dart';

const glProfitLossRoute = '/accounts/gl/profit-loss';

/// Accounts → GL → Profit & Loss. P&L derived from posted journal lines. Mirrors
/// the Blazor ProfitAndLossGl.razor page. Read-only.
class GlProfitLossScreen extends ConsumerWidget {
  const GlProfitLossScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(glProfitLossProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Profit & Loss (GL)',
          subtitle: 'Income less expense from posted journal lines.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(glProfitLossProvider))],
        ),
        const AccountsRangeSelector(),
        Expanded(
          child: AsyncStateView<GlProfitAndLoss>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(glProfitLossProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(GlProfitAndLoss r) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        KpiGrid(children: [
          KpiCard(label: 'Income', value: money(r.totalIncome, 'Tk'), icon: Icons.trending_up, tint: Bo.successSoft),
          KpiCard(label: 'Expense', value: money(r.totalExpense, 'Tk'), icon: Icons.trending_down, tint: Bo.dangerSoft),
          KpiCard(label: 'Net Profit', value: money(r.netProfit, 'Tk'), icon: Icons.account_balance_wallet, tint: Bo.primaryTint),
        ]),
        const SizedBox(height: 16),
        _section('Income', r.income, r.totalIncome, Bo.success),
        const SizedBox(height: 12),
        _section('Expense', r.expense, r.totalExpense, Bo.danger),
      ],
    );
  }

  Widget _section(String title, List<GlAccountLine> lines, double total, Color tone) {
    return SectionCard(
      title: title,
      child: Column(
        children: [
          for (final l in lines)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Row(children: [
                SizedBox(
                    width: 64,
                    child: Text(l.code, style: const TextStyle(color: Bo.textSubtle, fontSize: 13))),
                Expanded(child: Text(l.name)),
                Text(money(l.amount, 'Tk'), style: const TextStyle(fontWeight: FontWeight.w600)),
              ]),
            ),
          if (lines.isEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 6),
              child: Text('No entries.', style: TextStyle(color: Bo.textSubtle)),
            ),
          const Divider(),
          Row(children: [
            Expanded(child: Text('Total $title', style: const TextStyle(fontWeight: FontWeight.w800))),
            Text(money(total, 'Tk'), style: TextStyle(fontWeight: FontWeight.w800, color: tone)),
          ]),
        ],
      ),
    );
  }
}
