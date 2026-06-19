import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/app_page.dart';
import '../../dashboard/widgets.dart';
import '../accounts_models.dart';
import '../accounts_providers.dart';
import '../widgets.dart';

const balanceSheetRoute = '/accounts/gl/balance-sheet';

/// Accounts → GL → Balance Sheet. Assets vs Liabilities + Equity as of the range
/// end date. Mirrors the Blazor BalanceSheet.razor page. Read-only.
class BalanceSheetScreen extends ConsumerWidget {
  const BalanceSheetScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(balanceSheetProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Balance Sheet',
          subtitle: 'Assets vs liabilities and equity, as of the period end.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(balanceSheetProvider))],
        ),
        const AccountsRangeSelector(),
        Expanded(
          child: AsyncStateView<BalanceSheet>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(balanceSheetProvider),
            data: (b) => _body(b),
          ),
        ),
      ],
    );
  }

  Widget _body(BalanceSheet b) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        KpiGrid(children: [
          KpiCard(label: 'Total Assets', value: money(b.totalAssets, 'Tk'), icon: Icons.account_balance, tint: Bo.infoSoft),
          KpiCard(label: 'Total Liabilities', value: money(b.totalLiabilities, 'Tk'), icon: Icons.credit_card, tint: Bo.warningSoft),
          KpiCard(label: 'Total Equity', value: money(b.totalEquity, 'Tk'), icon: Icons.pie_chart, tint: Bo.primaryTint),
          KpiCard(
            label: 'Balanced',
            value: b.isBalanced ? 'Yes' : 'No',
            icon: b.isBalanced ? Icons.check_circle : Icons.error,
            tint: b.isBalanced ? Bo.successSoft : Bo.dangerSoft,
          ),
        ]),
        const SizedBox(height: 16),
        _section('Assets', b.assets, b.totalAssets, Bo.info),
        const SizedBox(height: 12),
        _section('Liabilities', b.liabilities, b.totalLiabilities, Bo.warning),
        const SizedBox(height: 12),
        _section('Equity', b.equity, b.totalEquity, Bo.primary,
            extra: ('Current-period earnings', b.currentEarnings)),
      ],
    );
  }

  Widget _section(String title, List<GlAccountLine> lines, double total, Color tone,
      {(String, double)? extra}) {
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
          if (extra != null)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Row(children: [
                const SizedBox(width: 64),
                Expanded(child: Text(extra.$1, style: const TextStyle(fontStyle: FontStyle.italic, color: Bo.textMuted))),
                Text(money(extra.$2, 'Tk'),
                    style: const TextStyle(fontWeight: FontWeight.w600, color: Bo.textMuted)),
              ]),
            ),
          if (lines.isEmpty && extra == null)
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
