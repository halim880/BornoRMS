import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const vatReportRoute = '/accounts/reports/vat';

/// Accounts → Reports → VAT. Output VAT collected over a date range, by rate.
/// Mirrors the Blazor VatReport.razor page. Read-only.
class VatReportScreen extends ConsumerWidget {
  const VatReportScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(vatReportProvider);

    return Column(
      children: [
        PageHeader(
          title: 'VAT Report',
          subtitle: 'Output VAT collected over a date range, grouped by rate.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(vatReportProvider))],
        ),
        const AccountsRangeSelector(),
        Expanded(
          child: AsyncStateView<VatReport>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(vatReportProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(VatReport r) {
    final cur = r.currency;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Taxable Sales', value: money(r.totalTaxable, cur), icon: Icons.point_of_sale, tint: Bo.infoSoft),
            KpiCard(label: 'Total VAT', value: money(r.totalVat, cur), icon: Icons.receipt, tint: Bo.primaryTint),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No taxable sales in this period.',
            columns: const [
              DataColumn(label: Text('Rate %'), numeric: true),
              DataColumn(label: Text('Taxable Sales'), numeric: true),
              DataColumn(label: Text('VAT'), numeric: true),
            ],
            rows: [
              for (final row in r.rows)
                DataRow(cells: [
                  DataCell(Text('${row.ratePercent.toStringAsFixed(2)}%')),
                  DataCell(Text(money(row.taxableSales, cur))),
                  DataCell(Text(money(row.vat, cur), style: const TextStyle(fontWeight: FontWeight.w600))),
                ]),
              if (r.rows.isNotEmpty)
                DataRow(
                  color: WidgetStatePropertyAll(Bo.bgSoft),
                  cells: [
                    const DataCell(Text('Total', style: TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(r.totalTaxable, cur),
                        style: const TextStyle(fontWeight: FontWeight.w800))),
                    DataCell(Text(money(r.totalVat, cur),
                        style: const TextStyle(fontWeight: FontWeight.w800, color: Bo.primary))),
                  ],
                ),
            ],
          ),
        ),
      ],
    );
  }
}
