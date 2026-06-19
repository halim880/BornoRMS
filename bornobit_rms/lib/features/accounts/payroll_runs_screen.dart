import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const payrollRunsRoute = '/accounts/payroll/runs';

const _pageSize = 14;

const _monthNames = [
  '', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
];

/// Accounts → Payroll → Runs. Monthly payroll runs and their net totals. Mirrors
/// the Blazor PayrollRuns.razor page. Read-only here.
class PayrollRunsScreen extends ConsumerStatefulWidget {
  const PayrollRunsScreen({super.key});

  @override
  ConsumerState<PayrollRunsScreen> createState() => _PayrollRunsScreenState();
}

class _PayrollRunsScreenState extends ConsumerState<PayrollRunsScreen> {
  int _page = 1;

  String _period(int year, int month) =>
      '${(month >= 1 && month <= 12) ? _monthNames[month] : month} $year';

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(payrollRunsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Payroll Runs',
          subtitle: 'Monthly payroll runs with their net pay totals.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(payrollRunsProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<PayrollRunSummary>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(payrollRunsProvider),
            data: (rows) => _table(rows),
          ),
        ),
      ],
    );
  }

  Widget _table(List<PayrollRunSummary> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: 'No payroll runs yet.',
      columns: const [
        DataColumn(label: Text('Run #')),
        DataColumn(label: Text('Period')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Net Pay'), numeric: true),
      ],
      rows: [
        for (final r in rows)
          DataRow(cells: [
            DataCell(Text(r.runNumber, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(_period(r.year, r.month))),
            DataCell(ToneChip(r.status, accountsStatusTone(r.status))),
            DataCell(Text(money(r.totalNet, 'Tk'),
                style: const TextStyle(fontWeight: FontWeight.w700))),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} runs',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }
}
