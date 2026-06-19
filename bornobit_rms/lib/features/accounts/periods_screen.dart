import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const periodsRoute = '/accounts/periods';

const _pageSize = 14;

const _monthNames = [
  '', 'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
];

/// Accounts → Periods. Fiscal (calendar month) periods and their open/closed
/// status. Mirrors the Blazor Periods.razor page. Read-only here.
class PeriodsScreen extends ConsumerStatefulWidget {
  const PeriodsScreen({super.key});

  @override
  ConsumerState<PeriodsScreen> createState() => _PeriodsScreenState();
}

class _PeriodsScreenState extends ConsumerState<PeriodsScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(periodsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Fiscal Periods',
          subtitle: 'Calendar-month periods. Closed months reject new postings.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(periodsProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<FiscalPeriod>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(periodsProvider),
            data: (rows) => _table(rows),
          ),
        ),
      ],
    );
  }

  String _monthLabel(int m) => (m >= 1 && m <= 12) ? _monthNames[m] : '$m';

  Widget _table(List<FiscalPeriod> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: 'No fiscal periods recorded. Months with no row are implicitly open.',
      columns: const [
        DataColumn(label: Text('Period')),
        DataColumn(label: Text('Year'), numeric: true),
        DataColumn(label: Text('Month')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Closed')),
        DataColumn(label: Text('Closed by')),
      ],
      rows: [
        for (final p in rows)
          DataRow(cells: [
            DataCell(Text(p.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text('${p.year}')),
            DataCell(Text(_monthLabel(p.month))),
            DataCell(ToneChip(p.status, accountsStatusTone(p.status))),
            DataCell(Text(p.closedAtUtc == null ? '—' : shortDate(p.closedAtUtc!),
                style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(p.closedBy?.isNotEmpty == true ? p.closedBy! : '—',
                style: const TextStyle(color: Bo.textSubtle))),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} periods',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }
}
