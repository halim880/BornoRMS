import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const employeesRoute = '/accounts/payroll/employees';

const _pageSize = 14;

/// Accounts → Payroll → Employees. Mirrors the Blazor Employees.razor page.
/// Read-only here.
class EmployeesScreen extends ConsumerStatefulWidget {
  const EmployeesScreen({super.key});

  @override
  ConsumerState<EmployeesScreen> createState() => _EmployeesScreenState();
}

class _EmployeesScreenState extends ConsumerState<EmployeesScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(employeesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Employees',
          subtitle: 'Staff roster used to seed payroll runs.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(employeesProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<Employee>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(employeesProvider),
            data: (rows) => _table(rows),
          ),
        ),
      ],
    );
  }

  Widget _table(List<Employee> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: 'No employees yet.',
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Designation')),
        DataColumn(label: Text('Base Salary'), numeric: true),
        DataColumn(label: Text('Overtime Rate'), numeric: true),
        DataColumn(label: Text('Joined')),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final e in rows)
          DataRow(cells: [
            DataCell(Text(e.code, style: const TextStyle(color: Bo.textSubtle))),
            DataCell(Text(e.fullName, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(e.designation?.isNotEmpty == true ? e.designation! : '—',
                style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(money(e.baseSalary, 'Tk'))),
            DataCell(Text(money(e.overtimeRate, 'Tk'))),
            DataCell(Text(shortDate(e.joinedOn))),
            DataCell(ToneChip(e.status, accountsStatusTone(e.status))),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} employees',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }
}
