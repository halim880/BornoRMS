import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import '../stock/purchase_return_form.dart';
import 'accounts_api.dart';
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
          subtitle: 'What we owe each supplier — goods received, less returns and payments.',
          actions: [
            OutlinedButton.icon(
              onPressed: () async {
                final done = await showPurchaseReturnForm(context);
                if (done == true) ref.invalidate(payablesProvider);
              },
              icon: const Icon(Icons.assignment_return, size: 16),
              label: const Text('Return goods'),
            ),
            const SizedBox(width: 8),
            RefreshAction(onPressed: () => ref.invalidate(payablesProvider)),
          ],
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
              DataColumn(label: Text('Returned'), numeric: true),
              DataColumn(label: Text('Paid'), numeric: true),
              DataColumn(label: Text('Outstanding'), numeric: true),
              DataColumn(label: Text('')),
            ],
            rows: [
              for (final r in rows)
                DataRow(cells: [
                  DataCell(Text(r.supplierCode, style: const TextStyle(color: Bo.textSubtle))),
                  DataCell(Text(r.supplierName, style: const TextStyle(fontWeight: FontWeight.w700))),
                  DataCell(Text('${r.paymentTermsDays}')),
                  DataCell(Text(money(r.received, 'Tk'))),
                  DataCell(Text(r.returned == 0 ? '—' : money(r.returned, 'Tk'),
                      style: TextStyle(color: r.returned > 0 ? Bo.warning : Bo.textSubtle))),
                  DataCell(Text(money(r.paid, 'Tk'))),
                  DataCell(Text(money(r.outstanding, 'Tk'),
                      style: TextStyle(
                          fontWeight: FontWeight.w700,
                          color: r.outstanding > 0 ? Bo.danger : Bo.success))),
                  DataCell(Row(mainAxisSize: MainAxisSize.min, children: [
                    IconButton(
                      tooltip: 'Payment history',
                      icon: const Icon(Icons.history, size: 18),
                      onPressed: () => _showHistory(r),
                    ),
                    IconButton(
                      tooltip: 'Return goods',
                      icon: const Icon(Icons.assignment_return, size: 18, color: Bo.warning),
                      onPressed: () async {
                        final done = await showPurchaseReturnForm(context, supplierId: r.supplierId);
                        if (done == true) ref.invalidate(payablesProvider);
                      },
                    ),
                    TextButton.icon(
                      onPressed: r.outstanding > 0 ? () => _recordPayment(r) : null,
                      icon: const Icon(Icons.payments, size: 16),
                      label: const Text('Pay'),
                    ),
                  ])),
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

  Future<void> _recordPayment(PayableRow r) async {
    final saved = await showDialog<bool>(context: context, builder: (_) => _RecordPaymentDialog(payable: r));
    if (saved == true) ref.invalidate(payablesProvider);
  }

  void _showHistory(PayableRow r) {
    showDialog<void>(context: context, builder: (_) => _PaymentHistoryDialog(payable: r));
  }
}

/// Records a payment against a supplier: amount (defaults to outstanding), cash account, date, reference.
class _RecordPaymentDialog extends ConsumerStatefulWidget {
  final PayableRow payable;
  const _RecordPaymentDialog({required this.payable});

  @override
  ConsumerState<_RecordPaymentDialog> createState() => _RecordPaymentDialogState();
}

class _RecordPaymentDialogState extends ConsumerState<_RecordPaymentDialog> {
  late final TextEditingController _amount =
      TextEditingController(text: widget.payable.outstanding.toStringAsFixed(2));
  final _reference = TextEditingController();
  String? _cashAccountId;
  DateTime _paidOn = DateTime.now();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _amount.dispose();
    _reference.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final amount = double.tryParse(_amount.text.trim());
    if (amount == null || amount <= 0) {
      setState(() => _error = 'Enter a valid amount.');
      return;
    }
    if (_cashAccountId == null) {
      setState(() => _error = 'Choose the cash account to pay from.');
      return;
    }
    setState(() { _busy = true; _error = null; });
    try {
      await ref.read(staffApiProvider).recordSupplierPayment(
            supplierId: widget.payable.supplierId,
            cashAccountId: _cashAccountId!,
            paidOn: _paidOn,
            amount: amount,
            reference: _reference.text.trim().isEmpty ? null : _reference.text.trim(),
          );
      ref.invalidate(supplierPaymentsProvider);
      if (mounted) {
        AppToast.show(context, 'Payment recorded');
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      setState(() { _busy = false; _error = e.toString(); });
    }
  }

  @override
  Widget build(BuildContext context) {
    final accounts = ref.watch(cashAccountsProvider);
    return AlertDialog(
      title: Text('Pay ${widget.payable.supplierName}'),
      content: SizedBox(
        width: 400,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Outstanding: ${money(widget.payable.outstanding, 'Tk')}',
                style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
            const SizedBox(height: 12),
            TextField(
              controller: _amount,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(labelText: 'Amount', prefixText: 'Tk '),
            ),
            const SizedBox(height: 8),
            accounts.when(
              loading: () => const LinearProgressIndicator(),
              error: (e, _) => Text('Cash accounts: $e', style: const TextStyle(color: Bo.danger)),
              data: (list) => DropdownButtonFormField<String>(
                initialValue: _cashAccountId,
                decoration: const InputDecoration(labelText: 'Pay from'),
                items: [for (final c in list) DropdownMenuItem(value: c.id, child: Text(c.name))],
                onChanged: (v) => setState(() => _cashAccountId = v),
              ),
            ),
            const SizedBox(height: 8),
            TextField(controller: _reference, decoration: const InputDecoration(labelText: 'Reference (optional)')),
            const SizedBox(height: 8),
            InputDecorator(
              decoration: const InputDecoration(labelText: 'Paid on', isDense: true),
              child: Row(children: [
                Expanded(child: Text(shortDate(_paidOn))),
                InkWell(
                  onTap: () async {
                    final picked = await showDatePicker(
                      context: context, initialDate: _paidOn, firstDate: DateTime(2020), lastDate: DateTime.now());
                    if (picked != null) setState(() => _paidOn = picked);
                  },
                  child: const Icon(Icons.calendar_today, size: 16),
                ),
              ]),
            ),
            if (_error != null) ...[
              const SizedBox(height: 10),
              Text(_error!, style: const TextStyle(color: Bo.danger, fontSize: 12)),
            ],
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: _busy ? null : () => Navigator.of(context).pop(false), child: const Text('Cancel')),
        FilledButton(
          onPressed: _busy ? null : _save,
          child: _busy
              ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Record payment'),
        ),
      ],
    );
  }
}

/// Payment history for a single supplier (statement view).
class _PaymentHistoryDialog extends ConsumerWidget {
  final PayableRow payable;
  const _PaymentHistoryDialog({required this.payable});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Dialog(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 520, maxHeight: 600),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 14, 8, 14),
              child: Row(children: [
                Expanded(child: Text('Payments · ${payable.supplierName}',
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800))),
                IconButton(onPressed: () => Navigator.of(context).pop(), icon: const Icon(Icons.close)),
              ]),
            ),
            const Divider(height: 1),
            Flexible(
              child: FutureBuilder<List<SupplierPaymentRow>>(
                future: ref.read(staffApiProvider).supplierPayments(supplierId: payable.supplierId),
                builder: (context, snap) {
                  if (snap.connectionState != ConnectionState.done) {
                    return const SizedBox(height: 160, child: Center(child: CircularProgressIndicator()));
                  }
                  if (snap.hasError) {
                    return Padding(padding: const EdgeInsets.all(24), child: Text('${snap.error}', style: const TextStyle(color: Bo.danger)));
                  }
                  final rows = snap.data ?? const [];
                  if (rows.isEmpty) {
                    return const Padding(padding: EdgeInsets.all(24), child: Text('No payments yet.', style: TextStyle(color: Bo.textSubtle)));
                  }
                  return ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      for (final p in rows)
                        Padding(
                          padding: const EdgeInsets.symmetric(vertical: 6),
                          child: Row(children: [
                            Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                              Text(shortDate(p.paidOn), style: const TextStyle(fontWeight: FontWeight.w600)),
                              if (p.cashAccountName != null || p.reference != null)
                                Text([if (p.cashAccountName != null) p.cashAccountName, if (p.reference != null) p.reference].join(' · '),
                                    style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                            ])),
                            Text(money(p.amount, 'Tk'), style: const TextStyle(fontWeight: FontWeight.w700)),
                          ]),
                        ),
                    ],
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
