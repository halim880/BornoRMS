import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/widgets/app_page.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'cash_ledger_view.dart';
import 'widgets.dart';

const cashBookRoute = '/accounts/reports/cash-book';

/// Accounts → Reports → Cash Book. Combined cash ledger across every cash account
/// over a date range. Mirrors the Blazor CashBook.razor page. Read-only.
class CashBookScreen extends ConsumerWidget {
  const CashBookScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(cashBookProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Cash Book',
          subtitle: 'Combined cash receipts and payments across all accounts.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(cashBookProvider))],
        ),
        const AccountsRangeSelector(),
        Expanded(
          child: AsyncStateView<CashLedger>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(cashBookProvider),
            data: (l) => CashLedgerView(ledger: l, showAccountColumn: true),
          ),
        ),
      ],
    );
  }
}
