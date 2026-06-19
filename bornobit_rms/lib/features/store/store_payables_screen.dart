import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import '../reports/widgets.dart';
import 'store_models.dart';
import 'store_providers.dart';

const storePayablesRoute = '/store/payables';

/// Store → Supplier Payables. Billed − paid per supplier. Mirrors StorePayables.razor.
class StorePayablesScreen extends ConsumerWidget {
  const StorePayablesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(storePayablesProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Supplier Payables',
          subtitle: 'Outstanding balance per supplier (billed − paid).',
          actions: [RefreshAction(onPressed: () => ref.invalidate(storePayablesProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<StoreSupplierPayable>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(storePayablesProvider),
            data: _body,
          ),
        ),
      ],
    );
  }

  Widget _body(List<StoreSupplierPayable> rows) {
    const cur = 'Tk';
    final totalOutstanding = rows.fold<double>(0, (a, r) => a + r.outstanding);
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
          child: KpiGrid(children: [
            KpiCard(
                label: 'Suppliers',
                value: count(rows.length),
                icon: Icons.local_shipping,
                tint: Bo.infoSoft),
            KpiCard(
                label: 'Total Outstanding',
                value: money(totalOutstanding, cur),
                icon: Icons.account_balance_wallet,
                tint: Bo.dangerSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No supplier balances yet.',
            columns: const [
              DataColumn(label: Text('Code')),
              DataColumn(label: Text('Supplier')),
              DataColumn(label: Text('Phone')),
              DataColumn(label: Text('Billed'), numeric: true),
              DataColumn(label: Text('Paid'), numeric: true),
              DataColumn(label: Text('Outstanding'), numeric: true),
            ],
            rows: [
              for (final r in rows)
                DataRow(cells: [
                  DataCell(Text(r.code)),
                  DataCell(Text(r.name, style: const TextStyle(fontWeight: FontWeight.w600))),
                  DataCell(Text(r.phone ?? '—')),
                  DataCell(Text(money(r.billed, cur))),
                  DataCell(Text(money(r.paid, cur))),
                  DataCell(Text(money(r.outstanding, cur),
                      style: TextStyle(
                          color: r.outstanding > 0 ? Bo.danger : Bo.text,
                          fontWeight: FontWeight.w700))),
                ]),
            ],
          ),
        ),
      ],
    );
  }
}
