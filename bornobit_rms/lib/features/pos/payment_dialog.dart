import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../dashboard/widgets.dart';
import 'pos_providers.dart';

const _methods = ['Cash', 'Card', 'Mobile'];
const _providers = ['Bkash', 'Nagad', 'Rocket', 'Upay', 'Other'];
const _cardTypes = ['Visa', 'Mastercard', 'Amex', 'Other'];

// Display labels for providers (payload keeps the canonical value above).
const _providerLabel = {
  'Bkash': 'bKash',
  'Nagad': 'Nagad',
  'Rocket': 'Rocket',
  'Upay': 'Upay',
  'Other': 'Other',
};

class _Tender {
  final String method;
  final String? provider;
  final double amount;
  final double tendered;
  final String? cardType;
  final String? reference;
  final String? txnId;
  _Tender(this.method, this.provider, this.amount, this.tendered,
      {this.cardType, this.reference, this.txnId});
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
  String _cardType = 'Visa';
  bool _discountPercent = true;
  final _amount = TextEditingController();
  final _reference = TextEditingController();
  final _txnId = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    // Live recompute of change / remaining as the cashier types.
    _amount.addListener(_onChange);
  }

  void _onChange() {
    if (mounted) setState(() {});
  }

  @override
  void dispose() {
    _amount.removeListener(_onChange);
    _amount.dispose();
    _reference.dispose();
    _txnId.dispose();
    super.dispose();
  }

  double get _stagedSum => _staged.fold(0.0, (a, t) => a + t.amount);
  double get _entered => double.tryParse(_amount.text) ?? 0;

  Future<void> _guard(Future<void> Function() body) async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await body();
      if (mounted) setState(() => _busy = false);
    } catch (e) {
      if (mounted) {
        setState(() {
          _busy = false;
          _error = e.toString();
        });
      }
    }
  }

  /// Builds the current (un-staged) tender from the entry fields, or null if empty.
  /// For Cash the entered value is treated as cash *received*: the applied amount
  /// clamps to [remaining] and the surplus becomes change.
  _Tender? _entry(double remaining) {
    final amt = _entered;
    if (amt <= 0) return null;
    final applied = amt > remaining ? remaining : amt;
    final tendered = _method == 'Cash' ? amt : applied;
    final ref = _reference.text.trim();
    final txn = _txnId.text.trim();
    return _Tender(
      _method,
      _method == 'Mobile' ? _provider : null,
      applied,
      tendered < applied ? applied : tendered,
      cardType: _method == 'Card' ? _cardType : null,
      reference: ref.isEmpty ? null : ref,
      txnId: _method == 'Mobile' && txn.isNotEmpty ? txn : null,
    );
  }

  void _clearEntry() {
    _amount.clear();
    _reference.clear();
    _txnId.clear();
  }

  void _addTender(double remaining) {
    final t = _entry(remaining);
    if (t == null) return;
    setState(() {
      _staged.add(t);
      _clearEntry();
    });
  }

  Map<String, dynamic> _payload(_Tender t) => <String, dynamic>{
        'method': t.method,
        if (t.provider != null) 'provider': t.provider,
        'amount': t.amount,
        'tendered': t.tendered,
        // Extra keys; persisted only if the backend add-payment DTO accepts them,
        // otherwise ignored by System.Text.Json. Display value either way.
        if (t.cardType != null) 'cardType': t.cardType,
        if (t.reference != null) 'reference': t.reference,
        if (t.txnId != null) 'transactionId': t.txnId,
      };

  Future<void> _settle(double remaining) => _guard(() async {
        final tenders = [..._staged];
        final pending = _entry(remaining);
        if (pending != null) tenders.add(pending);
        if (tenders.isEmpty) {
          setState(() => _error = 'Add at least one payment');
          return;
        }
        final payload = tenders.map(_payload).toList();
        final result = await ref.read(posControllerProvider.notifier).addPayment(payload);
        if (result.isPaid) {
          if (mounted) Navigator.of(context).pop(result);
        } else {
          setState(() {
            _staged.clear();
            _clearEntry();
            _error = 'Partial payment recorded — balance ${money(result.balanceDue, '')}';
          });
        }
      });

  Future<void> _applyDiscount(double value) => _guard(
        () => ref.read(posControllerProvider.notifier).applyDiscount(
              percent: _discountPercent ? value : null,
              amount: _discountPercent ? null : value,
            ),
      );

  Future<void> _rounding(String mode) =>
      _guard(() => ref.read(posControllerProvider.notifier).applyRounding(mode));

  /// Suggested cash denominations ≥ the bill — adapts to any amount, not fixed.
  List<double> _quickCash(double remaining) {
    if (remaining <= 0) return const [];
    final r = remaining.ceilToDouble();
    double roundUp(double step) => (r / step).ceil() * step;
    final set = <double>{roundUp(10), roundUp(50), roundUp(100)};
    for (final note in const [50.0, 100.0, 200.0, 500.0, 1000.0]) {
      if (note >= r) set.add(note);
    }
    final list = set.where((v) => v >= r).toList()..sort();
    return list.take(4).toList();
  }

  @override
  Widget build(BuildContext context) {
    final detail = ref.watch(posControllerProvider).detail;
    if (detail == null) return const SizedBox.shrink();
    final cur = detail.currency;
    final grand = detail.grandTotal;
    final alreadyPaid = detail.amountPaid;
    final remaining =
        (grand - alreadyPaid - _stagedSum).clamp(0, double.infinity).toDouble();
    final locked = alreadyPaid > 0; // discount/rounding locked once any payment exists
    final wide = MediaQuery.of(context).size.width >= 760;

    final summary = _SummarySide(
      detail: detail,
      cur: cur,
      grand: grand,
      alreadyPaid: alreadyPaid,
      remaining: remaining,
      staged: _staged,
      onRemoveTender: (t) => setState(() => _staged.remove(t)),
      locked: locked,
      discountPercent: _discountPercent,
      busy: _busy,
      onDiscountModeChanged: (p) => setState(() => _discountPercent = p),
      onApplyDiscount: _applyDiscount,
      onRounding: _rounding,
    );

    final entry = _EntrySide(
      cur: cur,
      remaining: remaining,
      method: _method,
      onMethod: (m) => setState(() {
        _method = m;
        _error = null;
      }),
      provider: _provider,
      onProvider: (p) => setState(() => _provider = p),
      cardType: _cardType,
      onCardType: (c) => setState(() => _cardType = c),
      amount: _amount,
      reference: _reference,
      txnId: _txnId,
      entered: _entered,
      quickCash: _quickCash(remaining),
      onFillAmount: (v) {
        _amount.text = v.toStringAsFixed(2);
        _amount.selection =
            TextSelection.collapsed(offset: _amount.text.length);
      },
      onAddTender: () => _addTender(remaining),
      busy: _busy,
    );

    final body = wide
        ? Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(child: SingleChildScrollView(padding: const EdgeInsets.all(20), child: summary)),
              const VerticalDivider(width: 1),
              Expanded(child: SingleChildScrollView(padding: const EdgeInsets.all(20), child: entry)),
            ],
          )
        : SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              summary,
              const SizedBox(height: 16),
              const Divider(height: 1),
              const SizedBox(height: 16),
              entry,
            ]),
          );

    return Dialog(
      insetPadding: const EdgeInsets.all(16),
      backgroundColor: Bo.surface,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(Bo.radiusLg)),
      child: SizedBox(
        width: wide ? 820 : 460,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            _Header(orderNumber: detail.orderNumber),
            Flexible(child: body),
            _ActionBar(
              busy: _busy,
              error: _error,
              ready: remaining <= 0,
              onCancel: () => Navigator.of(context).pop(),
              onComplete: () => _settle(remaining),
            ),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Header
// ─────────────────────────────────────────────────────────────────────────────
class _Header extends StatelessWidget {
  const _Header({required this.orderNumber});
  final String orderNumber;
  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 16, 12, 16),
      decoration: const BoxDecoration(
        border: Border(bottom: BorderSide(color: Bo.border)),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: Bo.primaryTint,
              borderRadius: BorderRadius.circular(Bo.radiusMd),
            ),
            child: const Icon(Icons.point_of_sale, color: Bo.primary, size: 22),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Checkout',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: Bo.text)),
                Text(orderNumber,
                    style: const TextStyle(fontSize: 13, color: Bo.textSubtle)),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Cancel',
            icon: const Icon(Icons.close, color: Bo.textSubtle),
            onPressed: () => Navigator.of(context).pop(),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Left / summary side
// ─────────────────────────────────────────────────────────────────────────────
class _SummarySide extends StatelessWidget {
  const _SummarySide({
    required this.detail,
    required this.cur,
    required this.grand,
    required this.alreadyPaid,
    required this.remaining,
    required this.staged,
    required this.onRemoveTender,
    required this.locked,
    required this.discountPercent,
    required this.busy,
    required this.onDiscountModeChanged,
    required this.onApplyDiscount,
    required this.onRounding,
  });

  final dynamic detail;
  final String cur;
  final double grand;
  final double alreadyPaid;
  final double remaining;
  final List<_Tender> staged;
  final ValueChanged<_Tender> onRemoveTender;
  final bool locked;
  final bool discountPercent;
  final bool busy;
  final ValueChanged<bool> onDiscountModeChanged;
  final ValueChanged<double> onApplyDiscount;
  final ValueChanged<String> onRounding;

  @override
  Widget build(BuildContext context) {
    final ready = remaining <= 0;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Amount-due hero card.
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: ready ? Bo.successSoft : Bo.primaryTint,
            borderRadius: BorderRadius.circular(Bo.radiusLg),
            border: Border.all(color: ready ? Bo.success : Bo.primarySoft),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                ready ? 'READY TO SETTLE' : 'AMOUNT DUE',
                style: TextStyle(
                  fontSize: 13,
                  letterSpacing: 1.2,
                  fontWeight: FontWeight.w700,
                  color: ready ? Bo.success : Bo.primaryEmphasis,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                money(remaining, cur),
                style: TextStyle(
                  fontSize: 38,
                  fontWeight: FontWeight.bold,
                  height: 1.0,
                  color: ready ? Bo.success : Bo.primary,
                ),
              ),
              const SizedBox(height: 14),
              _miniRow('Subtotal', money(detail.subtotal, cur)),
              if (detail.discountAmount != 0)
                _miniRow('Discount', '-${money(detail.discountAmount, cur)}'),
              if (detail.roundingAdjustment != 0)
                _miniRow('Rounding', money(detail.roundingAdjustment, cur)),
              _miniRow('Grand total', money(grand, cur), strong: true),
              if (alreadyPaid > 0) _miniRow('Already paid', money(alreadyPaid, cur)),
            ],
          ),
        ),

        // Discount + rounding (locked once a payment exists).
        if (!locked) ...[
          const SizedBox(height: 16),
          const _SectionLabel('Adjustments'),
          const SizedBox(height: 8),
          Row(children: [
            const Text('Discount', style: TextStyle(fontWeight: FontWeight.w600, color: Bo.textMuted)),
            const Spacer(),
            ToggleButtons(
              isSelected: [discountPercent, !discountPercent],
              onPressed: (i) => onDiscountModeChanged(i == 0),
              borderRadius: BorderRadius.circular(Bo.radiusSm),
              constraints: const BoxConstraints(minHeight: 34, minWidth: 46),
              children: const [Text('%'), Text('Tk')],
            ),
          ]),
          const SizedBox(height: 8),
          Wrap(spacing: 8, runSpacing: 8, children: [
            for (final v in (discountPercent ? [5.0, 10, 15, 20] : [10.0, 20, 50, 100]))
              ActionChip(
                label: Text(discountPercent ? '${v.toInt()}%' : v.toStringAsFixed(0)),
                onPressed: busy ? null : () => onApplyDiscount(v.toDouble()),
              ),
            ActionChip(label: const Text('Clear'), onPressed: busy ? null : () => onApplyDiscount(0)),
          ]),
          const SizedBox(height: 10),
          Row(children: [
            const Text('Rounding', style: TextStyle(fontWeight: FontWeight.w600, color: Bo.textMuted)),
            const SizedBox(width: 10),
            for (final m in const ['Floor', 'Ceil', 'None'])
              Padding(
                padding: const EdgeInsets.only(right: 8),
                child: ActionChip(label: Text(m), onPressed: busy ? null : () => onRounding(m)),
              ),
          ]),
        ],

        // Staged tenders (split payments).
        if (staged.isNotEmpty) ...[
          const SizedBox(height: 16),
          const _SectionLabel('Payments'),
          const SizedBox(height: 8),
          for (final t in staged)
            Container(
              margin: const EdgeInsets.only(bottom: 8),
              padding: const EdgeInsets.fromLTRB(12, 8, 4, 8),
              decoration: BoxDecoration(
                color: Bo.slate50,
                borderRadius: BorderRadius.circular(Bo.radiusMd),
                border: Border.all(color: Bo.border),
              ),
              child: Row(children: [
                Icon(_methodIcon(t.method), size: 18, color: Bo.textMuted),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    t.provider != null ? _providerLabel[t.provider] ?? t.provider! : t.method,
                    style: const TextStyle(fontWeight: FontWeight.w600, color: Bo.text),
                  ),
                ),
                Text(money(t.amount, cur),
                    style: const TextStyle(fontWeight: FontWeight.w700, color: Bo.text)),
                IconButton(
                  icon: const Icon(Icons.close, size: 18, color: Bo.textSubtle),
                  onPressed: () => onRemoveTender(t),
                ),
              ]),
            ),
        ],
      ],
    );
  }

  Widget _miniRow(String label, String value, {bool strong = false}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
          Text(label,
              style: TextStyle(
                  fontSize: 13,
                  color: Bo.textMuted,
                  fontWeight: strong ? FontWeight.w700 : FontWeight.w400)),
          Text(value,
              style: TextStyle(
                  fontSize: 13,
                  color: Bo.text,
                  fontWeight: strong ? FontWeight.w800 : FontWeight.w600)),
        ]),
      );
}

// ─────────────────────────────────────────────────────────────────────────────
// Right / entry side
// ─────────────────────────────────────────────────────────────────────────────
class _EntrySide extends StatelessWidget {
  const _EntrySide({
    required this.cur,
    required this.remaining,
    required this.method,
    required this.onMethod,
    required this.provider,
    required this.onProvider,
    required this.cardType,
    required this.onCardType,
    required this.amount,
    required this.reference,
    required this.txnId,
    required this.entered,
    required this.quickCash,
    required this.onFillAmount,
    required this.onAddTender,
    required this.busy,
  });

  final String cur;
  final double remaining;
  final String method;
  final ValueChanged<String> onMethod;
  final String provider;
  final ValueChanged<String> onProvider;
  final String cardType;
  final ValueChanged<String> onCardType;
  final TextEditingController amount;
  final TextEditingController reference;
  final TextEditingController txnId;
  final double entered;
  final List<double> quickCash;
  final ValueChanged<double> onFillAmount;
  final VoidCallback onAddTender;
  final bool busy;

  @override
  Widget build(BuildContext context) {
    final change = (entered - remaining);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const _SectionLabel('Payment method'),
        const SizedBox(height: 10),
        Row(
          children: [
            for (final m in _methods) ...[
              Expanded(
                child: _MethodButton(
                  selected: method == m,
                  icon: _methodIcon(m),
                  label: _methodLabel(m),
                  onTap: () => onMethod(m),
                ),
              ),
              if (m != _methods.last) const SizedBox(width: 10),
            ],
          ],
        ),
        const SizedBox(height: 18),
        AnimatedSwitcher(
          duration: const Duration(milliseconds: 180),
          transitionBuilder: (child, anim) =>
              FadeTransition(opacity: anim, child: child),
          child: KeyedSubtree(
            key: ValueKey(method),
            child: _methodSection(change),
          ),
        ),
        const SizedBox(height: 14),
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: busy ? null : onAddTender,
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Add another payment'),
          ),
        ),
      ],
    );
  }

  Widget _methodSection(double change) {
    switch (method) {
      case 'Card':
        return Column(
          key: const ValueKey('card'),
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const _SectionLabel('Card type'),
            const SizedBox(height: 8),
            Wrap(spacing: 8, runSpacing: 8, children: [
              for (final c in _cardTypes)
                ChoiceChip(
                  label: Text(c),
                  selected: cardType == c,
                  onSelected: (_) => onCardType(c),
                ),
            ]),
            const SizedBox(height: 14),
            _amountField('Amount'),
            const SizedBox(height: 12),
            _textField(reference, 'Reference number'),
          ],
        );
      case 'Mobile':
        return Column(
          key: const ValueKey('mobile'),
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const _SectionLabel('Provider'),
            const SizedBox(height: 8),
            Wrap(spacing: 8, runSpacing: 8, children: [
              for (final p in _providers)
                ChoiceChip(
                  label: Text(_providerLabel[p] ?? p),
                  selected: provider == p,
                  onSelected: (_) => onProvider(p),
                ),
            ]),
            const SizedBox(height: 14),
            _amountField('Amount'),
            const SizedBox(height: 12),
            _textField(txnId, 'Transaction ID'),
            const SizedBox(height: 12),
            _textField(reference, 'Reference number'),
          ],
        );
      default: // Cash
        return Column(
          key: const ValueKey('cash'),
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _amountField('Amount received'),
            const SizedBox(height: 12),
            Wrap(spacing: 8, runSpacing: 8, children: [
              _QuickChip(label: 'Exact', onTap: () => onFillAmount(remaining)),
              for (final v in quickCash)
                _QuickChip(label: v.toStringAsFixed(0), onTap: () => onFillAmount(v)),
            ]),
            if (change > 0) ...[
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                decoration: BoxDecoration(
                  color: Bo.successSoft,
                  borderRadius: BorderRadius.circular(Bo.radiusLg),
                  border: Border.all(color: Bo.success),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('CHANGE',
                        style: TextStyle(
                            fontSize: 13,
                            letterSpacing: 1.2,
                            fontWeight: FontWeight.w700,
                            color: Bo.success)),
                    Text(
                      money(change, cur),
                      style: const TextStyle(
                          fontSize: 32, fontWeight: FontWeight.bold, color: Bo.success),
                    ),
                  ],
                ),
              ),
            ],
          ],
        );
    }
  }

  Widget _amountField(String label) => TextField(
        controller: amount,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
        style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w700, color: Bo.text),
        decoration: InputDecoration(
          labelText: label,
          isDense: false,
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 18),
          prefixIcon: const Icon(Icons.payments_outlined),
        ),
      );

  Widget _textField(TextEditingController c, String label) => TextField(
        controller: c,
        decoration: InputDecoration(labelText: label, isDense: true),
      );
}

// ─────────────────────────────────────────────────────────────────────────────
// Bottom action bar
// ─────────────────────────────────────────────────────────────────────────────
class _ActionBar extends StatelessWidget {
  const _ActionBar({
    required this.busy,
    required this.error,
    required this.ready,
    required this.onCancel,
    required this.onComplete,
  });
  final bool busy;
  final String? error;
  final bool ready;
  final VoidCallback onCancel;
  final VoidCallback onComplete;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: const BoxDecoration(
        border: Border(top: BorderSide(color: Bo.border)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (error != null) ...[
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              margin: const EdgeInsets.only(bottom: 12),
              decoration: BoxDecoration(
                color: Bo.dangerSoft,
                borderRadius: BorderRadius.circular(Bo.radiusMd),
              ),
              child: Row(children: [
                const Icon(Icons.error_outline, size: 18, color: Bo.danger),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(error!, style: const TextStyle(color: Bo.danger, fontSize: 13)),
                ),
              ]),
            ),
          ],
          Row(
            children: [
              Expanded(
                flex: 2,
                child: OutlinedButton(
                  onPressed: busy ? null : onCancel,
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size.fromHeight(54),
                    side: const BorderSide(color: Bo.borderStrong),
                    foregroundColor: Bo.textMuted,
                  ),
                  child: const Text('Cancel'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                flex: 5,
                child: FilledButton.icon(
                  onPressed: busy ? null : onComplete,
                  style: FilledButton.styleFrom(
                    minimumSize: const Size.fromHeight(54),
                    backgroundColor: Bo.primary,
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(Bo.radiusMd)),
                  ),
                  icon: busy
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(
                              strokeWidth: 2, color: Colors.white))
                      : const Icon(Icons.check_circle_outline),
                  label: Text(
                    busy ? 'Processing…' : 'Complete payment',
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Small shared widgets / helpers
// ─────────────────────────────────────────────────────────────────────────────
class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.text);
  final String text;
  @override
  Widget build(BuildContext context) => Text(
        text.toUpperCase(),
        style: const TextStyle(
          fontSize: 12,
          letterSpacing: 1.0,
          fontWeight: FontWeight.w700,
          color: Bo.textSubtle,
        ),
      );
}

class _MethodButton extends StatelessWidget {
  const _MethodButton({
    required this.selected,
    required this.icon,
    required this.label,
    required this.onTap,
  });
  final bool selected;
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? Bo.primary : Bo.surface,
      borderRadius: BorderRadius.circular(Bo.radiusMd),
      child: InkWell(
        borderRadius: BorderRadius.circular(Bo.radiusMd),
        onTap: onTap,
        child: Container(
          height: 64,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(Bo.radiusMd),
            border: Border.all(color: selected ? Bo.primary : Bo.border, width: 1.5),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, size: 22, color: selected ? Colors.white : Bo.textMuted),
              const SizedBox(height: 4),
              Text(
                label,
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: selected ? Colors.white : Bo.textMuted,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _QuickChip extends StatelessWidget {
  const _QuickChip({required this.label, required this.onTap});
  final String label;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) {
    return Material(
      color: Bo.primaryTint,
      borderRadius: BorderRadius.circular(Bo.radiusMd),
      child: InkWell(
        borderRadius: BorderRadius.circular(Bo.radiusMd),
        onTap: onTap,
        child: Container(
          constraints: const BoxConstraints(minWidth: 64, minHeight: 48),
          padding: const EdgeInsets.symmetric(horizontal: 16),
          alignment: Alignment.center,
          child: Text(
            label,
            style: const TextStyle(
                fontSize: 16, fontWeight: FontWeight.w700, color: Bo.primaryEmphasis),
          ),
        ),
      ),
    );
  }
}

IconData _methodIcon(String method) => switch (method) {
      'Card' => Icons.credit_card,
      'Mobile' => Icons.smartphone,
      _ => Icons.payments,
    };

String _methodLabel(String method) => switch (method) {
      'Card' => 'Card',
      'Mobile' => 'Mobile',
      _ => 'Cash',
    };
