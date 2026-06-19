import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/app_page.dart';
import '../../dashboard/widgets.dart';
import '../accounts_models.dart';
import '../accounts_providers.dart';
import '../widgets.dart';

const chartOfAccountsRoute = '/accounts/gl/chart';

/// Accounts → GL → Chart of Accounts. The account tree (roots → children).
/// Mirrors the Blazor ChartOfAccounts.razor page. Read-only.
class ChartOfAccountsScreen extends ConsumerWidget {
  const ChartOfAccountsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(chartOfAccountsProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Chart of Accounts',
          subtitle: 'The general-ledger account tree, grouped by type.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(chartOfAccountsProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<AccountNode>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(chartOfAccountsProvider),
            data: (roots) => roots.isEmpty
                ? const EmptyState(message: 'No accounts defined.')
                : Card(
                    margin: const EdgeInsets.all(16),
                    child: ListView(
                      padding: const EdgeInsets.symmetric(vertical: 8),
                      children: [
                        for (final n in roots) ..._nodeTiles(n, 0),
                      ],
                    ),
                  ),
          ),
        ),
      ],
    );
  }

  List<Widget> _nodeTiles(AccountNode n, int depth) {
    return [
      Padding(
        padding: EdgeInsets.fromLTRB(16.0 + depth * 20, 6, 16, 6),
        child: Row(
          children: [
            Icon(n.isPostable ? Icons.description_outlined : Icons.folder_outlined,
                size: 16, color: n.isPostable ? Bo.textSubtle : Bo.primary),
            const SizedBox(width: 8),
            SizedBox(
              width: 64,
              child: Text(n.code, style: const TextStyle(color: Bo.textSubtle, fontSize: 13)),
            ),
            Expanded(
              child: Text(n.name,
                  style: TextStyle(
                      fontWeight: n.isPostable ? FontWeight.w500 : FontWeight.w700,
                      color: n.isActive ? Bo.text : Bo.textSubtle)),
            ),
            ToneChip(n.accountType, _typeTone(n.accountType)),
            const SizedBox(width: 8),
            Text(n.normalBalance == 'Debit' ? 'Dr' : 'Cr',
                style: const TextStyle(color: Bo.textMuted, fontSize: 12, fontWeight: FontWeight.w600)),
          ],
        ),
      ),
      const Divider(height: 1),
      for (final c in n.children) ..._nodeTiles(c, depth + 1),
    ];
  }

  String _typeTone(String t) => switch (t) {
        'Asset' => 'info',
        'Liability' => 'warning',
        'Equity' => 'primary',
        'Income' => 'success',
        'Expense' => 'danger',
        _ => 'neutral',
      };
}
