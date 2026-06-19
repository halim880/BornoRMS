import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const profitLossRoute = '/accounts/reports/profit-loss';

/// Accounts → Reports → Profit & Loss (cash-basis). Mirrors the Blazor
/// ProfitAndLoss.razor page. Read-only.
class ProfitLossScreen extends ConsumerWidget {
  const ProfitLossScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(profitLossProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Profit & Loss',
          subtitle: 'Cash-basis P&L from the cash book over a date range.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(profitLossProvider))],
        ),
        const AccountsRangeSelector(),
        Expanded(
          child: AsyncStateView<ProfitAndLoss>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(profitLossProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(ProfitAndLoss r) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        KpiGrid(children: [
          KpiCard(label: 'Revenue', value: money(r.totalRevenue, 'Tk'), icon: Icons.trending_up, tint: Bo.successSoft),
          KpiCard(label: 'COGS', value: money(r.totalCogs, 'Tk'), icon: Icons.inventory_2, tint: Bo.warningSoft),
          KpiCard(label: 'Gross Profit', value: money(r.grossProfit, 'Tk'), icon: Icons.savings, tint: Bo.infoSoft),
          KpiCard(label: 'Net Profit', value: money(r.netProfit, 'Tk'), icon: Icons.account_balance_wallet, tint: Bo.primaryTint),
        ]),
        const SizedBox(height: 16),
        _section('Revenue', r.revenue, r.totalRevenue, Bo.success),
        const SizedBox(height: 12),
        if (r.cogs.isNotEmpty) ...[
          _section('Cost of Goods Sold', r.cogs, r.totalCogs, Bo.warning),
          const SizedBox(height: 12),
        ],
        _section('Operating Expenses', r.expenses, r.totalExpenses, Bo.danger),
      ],
    );
  }

  Widget _section(String title, List<PlLine> lines, double total, Color tone) {
    return SectionCard(
      title: title,
      child: Column(
        children: [
          for (final l in lines)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Row(
                children: [
                  Expanded(child: Text(l.categoryName)),
                  Text(money(l.amount, 'Tk'), style: const TextStyle(fontWeight: FontWeight.w600)),
                ],
              ),
            ),
          if (lines.isEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 6),
              child: Text('No entries.', style: TextStyle(color: Bo.textSubtle)),
            ),
          const Divider(),
          Row(
            children: [
              Expanded(child: Text(title == 'Revenue' ? 'Total Revenue' : 'Total',
                  style: const TextStyle(fontWeight: FontWeight.w800))),
              Text(money(total, 'Tk'),
                  style: TextStyle(fontWeight: FontWeight.w800, color: tone)),
            ],
          ),
        ],
      ),
    );
  }
}
