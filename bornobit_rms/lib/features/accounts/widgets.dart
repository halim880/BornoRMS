import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import 'accounts_providers.dart';

/// A row of preset date-range chips driving [accountsRangeProvider]. Shared by
/// every Accounts report / GL screen that takes a date range.
class AccountsRangeSelector extends ConsumerWidget {
  const AccountsRangeSelector({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selected = ref.watch(accountsRangeProvider);
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        children: [
          for (final r in DashboardRange.values)
            ChoiceChip(
              label: Text(r.label),
              selected: selected == r,
              onSelected: (_) => ref.read(accountsRangeProvider.notifier).state = r,
            ),
        ],
      ),
    );
  }
}

/// Compact refresh icon button used in Accounts page headers.
class RefreshAction extends StatelessWidget {
  final VoidCallback onPressed;
  const RefreshAction({super.key, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return IconButton(
      tooltip: 'Refresh',
      icon: const Icon(Icons.refresh, color: Bo.textMuted),
      onPressed: onPressed,
    );
  }
}

/// Semantic tone for an Income/Expense transaction type.
String financeTypeTone(String type) => type == 'Income' ? 'success' : 'danger';

/// Tone for a journal/period/run/asset/recon status string.
String accountsStatusTone(String status) => switch (status) {
      'Posted' || 'Completed' || 'Active' || 'Paid' => 'success',
      'Draft' || 'InProgress' || 'Open' => 'warning',
      'Approved' => 'info',
      'Void' || 'Disposed' || 'Inactive' || 'Closed' => 'neutral',
      'FullyDepreciated' => 'info',
      _ => 'neutral',
    };
