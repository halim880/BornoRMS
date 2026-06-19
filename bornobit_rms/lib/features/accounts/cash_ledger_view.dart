import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';

/// Shared body for the Cash Book and Account Ledger reports: opening/closing KPI
/// cards plus a running-balance table over the cash-ledger rows.
class CashLedgerView extends StatelessWidget {
  final CashLedger ledger;
  final bool showAccountColumn;
  const CashLedgerView({super.key, required this.ledger, this.showAccountColumn = false});

  @override
  Widget build(BuildContext context) {
    final l = ledger;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Opening', value: money(l.openingBalance, 'Tk'), icon: Icons.flag_outlined, tint: Bo.infoSoft),
            KpiCard(label: 'Receipts', value: money(l.totalReceipts, 'Tk'), icon: Icons.south_west, tint: Bo.successSoft),
            KpiCard(label: 'Payments', value: money(l.totalPayments, 'Tk'), icon: Icons.north_east, tint: Bo.dangerSoft),
            KpiCard(label: 'Closing', value: money(l.closingBalance, 'Tk'), icon: Icons.account_balance_wallet, tint: Bo.primaryTint),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No cash movements in this period.',
            columns: [
              const DataColumn(label: Text('Date')),
              const DataColumn(label: Text('Number')),
              const DataColumn(label: Text('Category')),
              if (showAccountColumn) const DataColumn(label: Text('Account')),
              const DataColumn(label: Text('In'), numeric: true),
              const DataColumn(label: Text('Out'), numeric: true),
              const DataColumn(label: Text('Balance'), numeric: true),
            ],
            rows: [
              for (final r in l.rows)
                DataRow(cells: [
                  DataCell(Text(shortDate(r.occurredOn))),
                  DataCell(Text(r.number, style: const TextStyle(fontWeight: FontWeight.w700))),
                  DataCell(Text(r.categoryName)),
                  if (showAccountColumn)
                    DataCell(Text(r.cashAccountName, style: const TextStyle(color: Bo.textMuted))),
                  DataCell(Text(r.inAmount > 0 ? money(r.inAmount, 'Tk') : '—',
                      style: TextStyle(color: r.inAmount > 0 ? Bo.success : Bo.textSubtle))),
                  DataCell(Text(r.outAmount > 0 ? money(r.outAmount, 'Tk') : '—',
                      style: TextStyle(color: r.outAmount > 0 ? Bo.danger : Bo.textSubtle))),
                  DataCell(Text(money(r.runningBalance, 'Tk'),
                      style: const TextStyle(fontWeight: FontWeight.w600))),
                ]),
            ],
          ),
        ),
      ],
    );
  }
}
