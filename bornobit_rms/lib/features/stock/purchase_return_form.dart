import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_toast.dart';
import '../accounts/accounts_providers.dart';
import 'stock_api.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

/// Return goods to a supplier. Stock is issued out at current average cost and the supplier payable
/// is reduced. Optionally [supplierId] preselects the supplier (e.g. opened from the Payables row).
Future<bool?> showPurchaseReturnForm(BuildContext context, {String? supplierId}) => showDialog<bool>(
      context: context,
      builder: (_) => _PurchaseReturnFormDialog(supplierId: supplierId),
    );

class _ReturnLine {
  String? itemId;
  String? unitId;
  final TextEditingController qty = TextEditingController();
  bool get isValid => itemId != null && unitId != null && (double.tryParse(qty.text.trim()) ?? 0) > 0;
  Map<String, dynamic> toJson() => {'itemId': itemId, 'qty': double.tryParse(qty.text.trim()) ?? 0, 'unitId': unitId};
  void dispose() => qty.dispose();
}

class _PurchaseReturnFormDialog extends ConsumerStatefulWidget {
  final String? supplierId;
  const _PurchaseReturnFormDialog({this.supplierId});

  @override
  ConsumerState<_PurchaseReturnFormDialog> createState() => _PurchaseReturnFormDialogState();
}

class _PurchaseReturnFormDialogState extends ConsumerState<_PurchaseReturnFormDialog> {
  String? _supplierId;
  final _reason = TextEditingController();
  final List<_ReturnLine> _lines = [_ReturnLine()];
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _supplierId = widget.supplierId;
  }

  @override
  void dispose() {
    _reason.dispose();
    for (final l in _lines) {
      l.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    if (_supplierId == null) {
      setState(() => _error = 'Choose a supplier.');
      return;
    }
    final valid = _lines.where((l) => l.isValid).toList();
    if (valid.isEmpty) {
      setState(() => _error = 'Add at least one line (item, quantity and unit).');
      return;
    }
    setState(() { _busy = true; _error = null; });
    try {
      await ref.read(staffApiProvider).stockCreatePurchaseReturn(
            supplierId: _supplierId!,
            reason: _reason.text.trim().isEmpty ? null : _reason.text.trim(),
            lines: valid.map((l) => l.toJson()).toList(),
          );
      ref.invalidate(payablesProvider);
      ref.invalidate(stockMovementsProvider);
      if (mounted) {
        AppToast.show(context, 'Goods returned to supplier');
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      setState(() { _busy = false; _error = e.toString(); });
    }
  }

  @override
  Widget build(BuildContext context) {
    final suppliers = ref.watch(stockSuppliersProvider);
    final items = ref.watch(stockAllItemsProvider);
    final units = ref.watch(stockUnitsProvider);

    return Dialog(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 680, maxHeight: 680),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 16, 12, 8),
              child: Row(children: [
                const Expanded(child: Text('Return goods to supplier',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800))),
                IconButton(onPressed: () => Navigator.of(context).pop(false), icon: const Icon(Icons.close)),
              ]),
            ),
            const Divider(height: 1),
            Flexible(
              child: ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  suppliers.when(
                    loading: () => const LinearProgressIndicator(),
                    error: (e, _) => Text('Suppliers: $e', style: const TextStyle(color: Bo.danger)),
                    data: (list) {
                      final active = list.where((s) => s.isActive).toList();
                      return DropdownButtonFormField<String>(
                        initialValue: _supplierId,
                        decoration: const InputDecoration(labelText: 'Supplier'),
                        items: [for (final s in active) DropdownMenuItem(value: s.id, child: Text(s.name))],
                        onChanged: _busy ? null : (v) => setState(() => _supplierId = v),
                      );
                    },
                  ),
                  const SizedBox(height: 12),
                  TextField(controller: _reason, decoration: const InputDecoration(labelText: 'Reason (optional)')),
                  const SizedBox(height: 16),
                  Row(children: [
                    const Text('Items to return', style: TextStyle(fontWeight: FontWeight.w700)),
                    const Spacer(),
                    TextButton.icon(
                      onPressed: _busy ? null : () => setState(() => _lines.add(_ReturnLine())),
                      icon: const Icon(Icons.add, size: 18),
                      label: const Text('Add line'),
                    ),
                  ]),
                  items.when(
                    loading: () => const LinearProgressIndicator(),
                    error: (e, _) => Text('Items: $e', style: const TextStyle(color: Bo.danger)),
                    data: (itemList) => units.when(
                      loading: () => const LinearProgressIndicator(),
                      error: (e, _) => Text('Units: $e', style: const TextStyle(color: Bo.danger)),
                      data: (unitList) => Column(
                        children: [for (final line in _lines) _row(line, itemList, unitList)],
                      ),
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text('Returned items are valued at their current average cost.',
                      style: TextStyle(color: Bo.textSubtle, fontSize: 12)),
                  if (_error != null) ...[
                    const SizedBox(height: 10),
                    Text(_error!, style: const TextStyle(color: Bo.danger, fontSize: 12)),
                  ],
                ],
              ),
            ),
            const Divider(height: 1),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Row(mainAxisAlignment: MainAxisAlignment.end, children: [
                TextButton(onPressed: _busy ? null : () => Navigator.of(context).pop(false), child: const Text('Cancel')),
                const SizedBox(width: 8),
                FilledButton(
                  style: FilledButton.styleFrom(backgroundColor: Bo.warning),
                  onPressed: _busy ? null : _save,
                  child: _busy
                      ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Text('Return goods'),
                ),
              ]),
            ),
          ],
        ),
      ),
    );
  }

  Widget _row(_ReturnLine line, List<StockItem> items, List<StockUnit> units) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
        Expanded(
          flex: 5,
          child: DropdownButtonFormField<String>(
            initialValue: line.itemId,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Item', isDense: true),
            items: [for (final it in items) DropdownMenuItem(value: it.id, child: Text(it.name, overflow: TextOverflow.ellipsis))],
            onChanged: (v) => setState(() {
              line.itemId = v;
              for (final it in items) {
                if (it.id == v) { line.unitId ??= it.baseUnitId; break; }
              }
            }),
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          flex: 2,
          child: TextField(
            controller: line.qty,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Qty', isDense: true),
            onChanged: (_) => setState(() {}),
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          flex: 2,
          child: DropdownButtonFormField<String>(
            initialValue: line.unitId,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Unit', isDense: true),
            items: [for (final u in units) DropdownMenuItem(value: u.id, child: Text(u.code))],
            onChanged: (v) => setState(() => line.unitId = v),
          ),
        ),
        IconButton(
          tooltip: 'Remove line',
          onPressed: _lines.length == 1 ? null : () => setState(() { _lines.remove(line); line.dispose(); }),
          icon: const Icon(Icons.remove_circle_outline, size: 20, color: Bo.danger),
        ),
      ]),
    );
  }
}
