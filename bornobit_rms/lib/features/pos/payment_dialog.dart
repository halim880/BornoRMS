import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../dashboard/widgets.dart';
import 'pos_providers.dart';

const _methods = ['Cash', 'Card', 'Mobile'];
const _providers = ['Bkash', 'Nagad', 'Rocket', 'Upay', 'Other'];

class _Tender {
  final String method;
  final String? provider;
  final double amount;
  final double tendered;
  _Tender(this.method, this.provider, this.amount, this.tendered);
}

/// Returns the SettlementResult when the order is fully paid, else null.
class PaymentDialog extends ConsumerStatefulWidget {
  const PaymentDialog({super.key});
  @override
  ConsumerState<PaymentDialog> createState() => _PaymentDialogState();
}

class _PaymentDialogState extends ConsumerState<PaymentDialog> {
  final List<_Tender> _staged = [];
  String _method = 'Cash';
  String _provider = 'Bkash';
  bool _discountPercent = true;
  final _amount = TextEditingController();
  final _tendered = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _amount.dispose();
    _tendered.dispose();
    super.dispose();
  }

  double get _stagedSum => _staged.fold(0.0, (a, t) => a + t.amount);

  Future<void> _guard(Future<void> Function() body) async {
    setState(() { _busy = true; _error = null; });
    try {
      await body();
      if (mounted) setState(() => _busy = false);
    } catch (e) {
      if (mounted) setState(() { _busy = false; _error = e.toString(); });
    }
  }

  void _addTender(double remaining) {
    final amt = double.tryParse(_amount.text) ?? 0;
    if (amt <= 0) return;
    final applied = amt > remaining ? remaining : amt;
    final tendered = _method == 'Cash' ? (double.tryParse(_tendered.text) ?? amt) : applied;
    setState(() {
      _staged.add(_Tender(_method, _method == 'Mobile' ? _provider : null, applied, tendered < applied ? applied : tendered));
      _amount.clear();
      _tendered.clear();
    });
  }

  Future<void> _settle(double remaining) => _guard(() async {
        // Fold any pending entry into the tenders.
        final pending = double.tryParse(_amount.text) ?? 0;
        final tenders = [..._staged];
        if (pending > 0) {
          final applied = pending > remaining ? remaining : pending;
          final tendered = _method == 'Cash' ? (double.tryParse(_tendered.text) ?? pending) : applied;
          tenders.add(_Tender(_method, _method == 'Mobile' ? _provider : null, applied, tendered < applied ? applied : tendered));
        }
        if (tenders.isEmpty) {
          setState(() => _error = 'Add at least one payment');
          return;
        }
        final payload = tenders
            .map((t) => <String, dynamic>{
                  'method': t.method,
                  if (t.provider != null) 'provider': t.provider,
                  'amount': t.amount,
                  'tendered': t.tendered,
                })
            .toList();
        final result = await ref.read(posControllerProvider.notifier).addPayment(payload);
        if (result.isPaid) {
          if (mounted) Navigator.of(context).pop(result);
        } else {
          setState(() {
            _staged.clear();
            _amount.clear();
            _tendered.clear();
            _error = 'Partial payment recorded — balance ${money(result.balanceDue, '')}';
          });
        }
      });

  Future<void> _applyDiscount(double value) =>
      _guard(() => ref.read(posControllerProvider.notifier).applyDiscount(
            percent: _discountPercent ? value : null,
            amount: _discountPercent ? null : value,
          ));

  Future<void> _rounding(String mode) =>
      _guard(() => ref.read(posControllerProvider.notifier).applyRounding(mode));

  @override
  Widget build(BuildContext context) {
    final detail = ref.watch(posControllerProvider).detail;
    if (detail == null) return const SizedBox.shrink();
    final cur = detail.currency;
    final grand = detail.grandTotal;
    final alreadyPaid = detail.amountPaid;
    final remaining = (grand - alreadyPaid - _stagedSum).clamp(0, double.infinity).toDouble();
    final locked = alreadyPaid > 0; // discount/rounding locked once any payment exists

    return AlertDialog(
      title: Text('Checkout · ${detail.orderNumber}'),
      content: SizedBox(
        width: 460,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Totals
              _row('Subtotal', money(detail.subtotal, cur)),
              if (detail.discountAmount != 0) _row('Discount', '-${money(detail.discountAmount, cur)}'),
              if (detail.roundingAdjustment != 0) _row('Rounding', money(detail.roundingAdjustment, cur)),
              _row('Grand total', money(grand, cur), bold: true),
              if (alreadyPaid > 0) _row('Already paid', money(alreadyPaid, cur)),
              _row('Remaining', money(remaining, cur), bold: true, color: Bo.primary),
              const Divider(),

              // Discount + rounding (locked after first payment)
              if (!locked) ...[
                Row(children: [
                  const Text('Discount', style: TextStyle(fontWeight: FontWeight.w600)),
                  const Spacer(),
                  ToggleButtons(
                    isSelected: [_discountPercent, !_discountPercent],
                    onPressed: (i) => setState(() => _discountPercent = i == 0),
                    constraints: const BoxConstraints(minHeight: 30, minWidth: 44),
                    children: const [Text('%'), Text('Tk')],
                  ),
                ]),
                const SizedBox(height: 6),
                Wrap(spacing: 6, children: [
                  for (final v in (_discountPercent ? [5.0, 10, 15, 20] : [10.0, 20, 50, 100]))
                    ActionChip(label: Text(_discountPercent ? '${v.toInt()}%' : v.toStringAsFixed(0), style: const TextStyle(fontSize: 12)), onPressed: _busy ? null : () => _applyDiscount(v.toDouble())),
                  ActionChip(label: const Text('Clear', style: TextStyle(fontSize: 12)), onPressed: _busy ? null : () => _applyDiscount(0)),
                ]),
                const SizedBox(height: 8),
                Row(children: [
                  const Text('Rounding', style: TextStyle(fontWeight: FontWeight.w600)),
                  const SizedBox(width: 8),
                  for (final m in const ['Floor', 'Ceil', 'None'])
                    Padding(
                      padding: const EdgeInsets.only(right: 6),
                      child: ActionChip(label: Text(m, style: const TextStyle(fontSize: 12)), onPressed: _busy ? null : () => _rounding(m)),
                    ),
                ]),
                const Divider(),
              ],

              // Tenders
              const Align(alignment: Alignment.centerLeft, child: Text('Payment', style: TextStyle(fontWeight: FontWeight.w700))),
              for (final t in _staged)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 2),
                  child: Row(children: [
                    Expanded(child: Text('${t.method}${t.provider != null ? ' · ${t.provider}' : ''}')),
                    Text(money(t.amount, cur)),
                    IconButton(icon: const Icon(Icons.close, size: 16), onPressed: () => setState(() => _staged.remove(t))),
                  ]),
                ),
              const SizedBox(height: 4),
              Wrap(spacing: 6, children: [
                for (final m in _methods)
                  ChoiceChip(label: Text(m), selected: _method == m, onSelected: (_) => setState(() => _method = m)),
              ]),
              if (_method == 'Mobile') ...[
                const SizedBox(height: 6),
                DropdownButtonFormField<String>(
                  initialValue: _provider,
                  decoration: const InputDecoration(labelText: 'Provider', isDense: true),
                  items: [for (final p in _providers) DropdownMenuItem(value: p, child: Text(p))],
                  onChanged: (v) => setState(() => _provider = v ?? 'Bkash'),
                ),
              ],
              const SizedBox(height: 8),
              Row(children: [
                Expanded(child: TextField(controller: _amount, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Amount', isDense: true))),
                const SizedBox(width: 8),
                OutlinedButton(onPressed: () => setState(() => _amount.text = remaining.toStringAsFixed(2)), child: const Text('Remaining')),
              ]),
              if (_method == 'Cash') ...[
                const SizedBox(height: 8),
                TextField(controller: _tendered, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Cash tendered (optional)', isDense: true)),
              ],
              const SizedBox(height: 6),
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(onPressed: _busy ? null : () => _addTender(remaining), icon: const Icon(Icons.add, size: 18), label: const Text('Add tender')),
              ),
              if (_error != null) Text(_error!, style: const TextStyle(color: Bo.danger, fontSize: 12)),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: _busy ? null : () => Navigator.of(context).pop(), child: const Text('Close')),
        FilledButton(
          onPressed: _busy ? null : () => _settle(remaining),
          child: _busy
              ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Settle'),
        ),
      ],
    );
  }

  Widget _row(String label, String value, {bool bold = false, Color? color}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
          Text(label, style: TextStyle(fontWeight: bold ? FontWeight.w700 : FontWeight.w400, color: color)),
          Text(value, style: TextStyle(fontWeight: bold ? FontWeight.w800 : FontWeight.w600, color: color)),
        ]),
      );
}
