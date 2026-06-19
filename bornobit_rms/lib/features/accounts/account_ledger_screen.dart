import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/widgets/app_page.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'cash_ledger_view.dart';
import 'widgets.dart';

const accountLedgerRoute = '/accounts/reports/ledger';

/// Accounts → Reports → Account Ledger. Single cash-account ledger with running
/// balance. Mirrors the Blazor AccountLedger.razor page. Read-only.
class AccountLedgerScreen extends ConsumerWidget {
  const AccountLedgerScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(accountLedgerProvider);
    final accountsAsync = ref.watch(cashAccountsProvider);
    final selected = ref.watch(ledgerAccountProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Account Ledger',
          subtitle: 'One cash account, with a running balance over a date range.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(accountLedgerProvider))],
        ),
        const AccountsRangeSelector(),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
          child: Row(
            children: [
              Expanded(
                child: accountsAsync.maybeWhen(
                  data: (accounts) => DropdownButtonFormField<String?>(
                    initialValue: selected,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Cash account'),
                    items: [
                      const DropdownMenuItem<String?>(value: null, child: Text('All accounts')),
                      for (final a in accounts)
                        DropdownMenuItem<String?>(value: a.id, child: Text(a.name)),
                    ],
                    onChanged: (v) => ref.read(ledgerAccountProvider.notifier).state = v,
                  ),
                  orElse: () => const SizedBox.shrink(),
                ),
              ),
            ],
          ),
        ),
        Expanded(
          child: AsyncStateView<CashLedger>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(accountLedgerProvider),
            data: (l) => CashLedgerView(ledger: l, showAccountColumn: selected == null),
          ),
        ),
      ],
    );
  }
}
