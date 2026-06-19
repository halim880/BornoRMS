import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/app_page.dart';
import '../../dashboard/widgets.dart';
import '../accounts_models.dart';
import '../accounts_providers.dart';
import '../widgets.dart';

const trialBalanceRoute = '/accounts/gl/trial-balance';

/// Accounts → GL → Trial Balance. Net balance per account over posted journal
/// lines. Mirrors the Blazor TrialBalance.razor page. Read-only.
class TrialBalanceScreen extends ConsumerWidget {
  const TrialBalanceScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(trialBalanceProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Trial Balance',
          subtitle: 'Net account balances from posted journal lines.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(trialBalanceProvider))],
        ),
        const AccountsRangeSelector(),
        Expanded(
          child: AsyncStateView<TrialBalance>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(trialBalanceProvider),
            data: (tb) => _body(tb),
          ),
        ),
      ],
    );
  }

  Widget _body(TrialBalance tb) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Total Debit', value: money(tb.totalDebit, 'Tk'), icon: Icons.remove, tint: Bo.infoSoft),
            KpiCard(label: 'Total Credit', value: money(tb.totalCredit, 'Tk'), icon: Icons.add, tint: Bo.primaryTint),
            KpiCard(
              label: 'Balanced',
              value: tb.isBalanced ? 'Yes' : 'No',
              icon: tb.isBalanced ? Icons.check_circle : Icons.error,
              tint: tb.isBalanced ? Bo.successSoft : Bo.dangerSoft,
            ),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No posted journal lines in this period.',
            columns: const [
              DataColumn(label: Text('Code')),
              DataColumn(label: Text('Account')),
              DataColumn(label: Text('Type')),
              DataColumn(label: Text('Debit'), numeric: true),
              DataColumn(label: Text('Credit'), numeric: true),
            ],
            rows: [
              for (final r in tb.rows)
                DataRow(cells: [
                  DataCell(Text(r.code, style: const TextStyle(color: Bo.textSubtle))),
                  DataCell(Text(r.name, style: const TextStyle(fontWeight: FontWeight.w600))),
                  DataCell(Text(r.accountType, style: const TextStyle(color: Bo.textMuted))),
                  DataCell(Text(r.debit > 0 ? money(r.debit, 'Tk') : '—')),
                  DataCell(Text(r.credit > 0 ? money(r.credit, 'Tk') : '—')),
                ]),
              if (tb.rows.isNotEmpty)
                DataRow(
                  color: WidgetStatePropertyAll(Bo.bgSoft),
                  cells: [
                    const DataCell(Text('')),
                    const DataCell(Text('Total', style: TextStyle(fontWeight: FontWeight.w800))),
                    const DataCell(Text('')),
                    DataCell(Text(money(tb.totalDebit, 'Tk'),
                        style: const TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(tb.totalCredit, 'Tk'),
                        style: const TextStyle(fontWeight: FontWeight.w800))),
                  ],
                ),
            ],
          ),
        ),
      ],
    );
  }
}
