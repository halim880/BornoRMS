import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'reports_models.dart';
import 'reports_providers.dart';
import 'widgets.dart';

const salesInvoiceReportRoute = '/operations/reports/sales-invoices';

/// Operations → Reports → Sales (Invoice-wise). One row per paid invoice.
/// Mirrors the Blazor SalesInvoiceReport.razor page.
class SalesInvoiceReportScreen extends ConsumerWidget {
  const SalesInvoiceReportScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(salesInvoiceReportProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Sales Report (Invoice-wise)',
          subtitle: 'Paid sales over a date range, one row per invoice.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(salesInvoiceReportProvider))],
        ),
        const ReportsRangeSelector(),
        Expanded(
          child: AsyncStateView<SalesInvoiceReport>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(salesInvoiceReportProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(SalesInvoiceReport r) {
    final cur = r.currency;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Invoices', value: count(r.totalInvoices), icon: Icons.receipt_long, tint: Bo.primaryTint),
            KpiCard(label: 'Subtotal', value: money(r.totalSubtotal, cur), icon: Icons.payments, tint: Bo.infoSoft),
            KpiCard(label: 'Discount', value: money(r.totalDiscount, cur), icon: Icons.sell, tint: Bo.dangerSoft),
            KpiCard(label: 'Total', value: money(r.grandTotal, cur), icon: Icons.account_balance_wallet, tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No paid sales in this period.',
            columns: const [
              DataColumn(label: Text('Date')),
              DataColumn(label: Text('Invoice #')),
              DataColumn(label: Text('Customer')),
              DataColumn(label: Text('Type')),
              DataColumn(label: Text('Method')),
              DataColumn(label: Text('Subtotal'), numeric: true),
              DataColumn(label: Text('Discount'), numeric: true),
              DataColumn(label: Text('Total'), numeric: true),
            ],
            rows: [
              for (final row in r.rows)
                DataRow(cells: [
                  DataCell(Text(dateTimeDmy(row.paidAtUtc))),
                  DataCell(Text(row.orderNumber, style: const TextStyle(fontWeight: FontWeight.w600))),
                  DataCell(Text(row.customerLabel)),
                  DataCell(ToneChip(row.orderType, _orderTypeTone(row.orderType))),
                  DataCell(row.paymentMethod == null
                      ? const Text('—', style: TextStyle(color: Bo.textSubtle))
                      : ToneChip(row.paymentMethod!, _paymentTone(row.paymentMethod!))),
                  DataCell(Text(money(row.subtotal, cur))),
                  DataCell(Text(
                    row.discount > 0 ? money(row.discount, cur) : '—',
                    style: const TextStyle(color: Bo.danger),
                  )),
                  DataCell(Text(money(row.total, cur),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                ]),
            ],
          ),
        ),
      ],
    );
  }

  String _orderTypeTone(String t) => switch (t) {
        'DineIn' => 'info',
        'Takeaway' => 'primary',
        'Delivery' => 'warning',
        _ => 'neutral',
      };

  String _paymentTone(String m) => switch (m) {
        'Cash' => 'success',
        'Card' => 'info',
        'Mobile' || 'MobileBanking' => 'primary',
        _ => 'neutral',
      };
}
