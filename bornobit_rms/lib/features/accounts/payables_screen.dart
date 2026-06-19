import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const payablesRoute = '/accounts/payables';

const _pageSize = 14;

/// Accounts → Payables. Accounts payable per supplier (received vs paid). Mirrors
/// the Blazor Payables.razor page. Read-only here.
class PayablesScreen extends ConsumerStatefulWidget {
  const PayablesScreen({super.key});

  @override
  ConsumerState<PayablesScreen> createState() => _PayablesScreenState();
}

class _PayablesScreenState extends ConsumerState<PayablesScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(payablesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Payables',
          subtitle: 'What we owe each supplier — goods received vs paid.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(payablesProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<PayableRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(payablesProvider),
            data: (rows) => _table(rows),
          ),
        ),
      ],
    );
  }

  Widget _table(List<PayableRow> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();
    final outstanding = all.fold<double>(0, (s, r) => s + r.outstanding);

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Suppliers', value: count(all.length), icon: Icons.local_shipping, tint: Bo.primaryTint),
            KpiCard(label: 'Outstanding', value: money(outstanding, 'Tk'), icon: Icons.payments, tint: Bo.dangerSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No supplier payables.',
            columns: const [
              DataColumn(label: Text('Code')),
              DataColumn(label: Text('Supplier')),
              DataColumn(label: Text('Terms (days)'), numeric: true),
              DataColumn(label: Text('Received'), numeric: true),
              DataColumn(label: Text('Paid'), numeric: true),
              DataColumn(label: Text('Outstanding'), numeric: true),
            ],
            rows: [
              for (final r in rows)
                DataRow(cells: [
                  DataCell(Text(r.supplierCode, style: const TextStyle(color: Bo.textSubtle))),
                  DataCell(Text(r.supplierName, style: const TextStyle(fontWeight: FontWeight.w700))),
                  DataCell(Text('${r.paymentTermsDays}')),
                  DataCell(Text(money(r.received, 'Tk'))),
                  DataCell(Text(money(r.paid, 'Tk'))),
                  DataCell(Text(money(r.outstanding, 'Tk'),
                      style: TextStyle(
                          fontWeight: FontWeight.w700,
                          color: r.outstanding > 0 ? Bo.danger : Bo.success))),
                ]),
            ],
            pager: Pager(
              page: page,
              totalPages: totalPages,
              label: '${all.length} suppliers',
              onPage: (p) => setState(() => _page = p),
            ),
          ),
        ),
      ],
    );
  }
}
