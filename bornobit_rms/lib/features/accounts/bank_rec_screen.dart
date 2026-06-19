import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const bankRecRoute = '/accounts/bank-rec';

const _pageSize = 14;

/// Accounts → Bank Reconciliation. Statement reconciliations per bank account.
/// Mirrors the Blazor BankReconciliation.razor page. Read-only here.
class BankRecScreen extends ConsumerStatefulWidget {
  const BankRecScreen({super.key});

  @override
  ConsumerState<BankRecScreen> createState() => _BankRecScreenState();
}

class _BankRecScreenState extends ConsumerState<BankRecScreen> {
  int _page = 1;

  String _statusLabel(String s) => s == 'InProgress' ? 'In Progress' : s;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(bankReconciliationsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Bank Reconciliation',
          subtitle: 'Reconcile each bank account against its statement balance.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(bankReconciliationsProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<BankReconciliation>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(bankReconciliationsProvider),
            data: (rows) => _table(rows),
          ),
        ),
      ],
    );
  }

  Widget _table(List<BankReconciliation> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: 'No reconciliations yet.',
      columns: const [
        DataColumn(label: Text('Account')),
        DataColumn(label: Text('Statement Date')),
        DataColumn(label: Text('Statement Bal.'), numeric: true),
        DataColumn(label: Text('Cleared Bal.'), numeric: true),
        DataColumn(label: Text('Difference'), numeric: true),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Completed')),
      ],
      rows: [
        for (final r in rows)
          DataRow(cells: [
            DataCell(Text(r.cashAccountName, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(shortDate(r.statementDate))),
            DataCell(Text(money(r.statementBalance, 'Tk'))),
            DataCell(Text(money(r.clearedBalance, 'Tk'))),
            DataCell(Builder(builder: (_) {
              final diff = r.statementBalance - r.clearedBalance;
              return Text(money(diff, 'Tk'),
                  style: TextStyle(
                      fontWeight: FontWeight.w700,
                      color: diff == 0 ? Bo.success : Bo.danger));
            })),
            DataCell(ToneChip(_statusLabel(r.status), accountsStatusTone(r.status))),
            DataCell(Text(r.completedOn == null ? '—' : shortDate(r.completedOn!),
                style: const TextStyle(color: Bo.textMuted))),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} reconciliations',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }
}
