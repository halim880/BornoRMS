import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart' show shortDate;
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart' show money;
import 'stock_api.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

/// One editable PO/GRN line: an item + qty + unit + unit cost. Holds its own
/// controllers; [toJson] yields the API line map. Shared shape between PO and GRN.
class PoLineDraft {
  String? itemId;
  String? unitId;
  final String? poLineId; // set when receiving against a purchase-order line
  final TextEditingController qty;
  final TextEditingController cost;

  PoLineDraft({this.itemId, this.unitId, this.poLineId, double? qty, double? cost})
      : qty = TextEditingController(text: qty == null ? '' : _trim(qty)),
        cost = TextEditingController(text: cost == null ? '' : _trim(cost));

  static String _trim(double v) => v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toString();

  double get qtyValue => double.tryParse(qty.text.trim()) ?? 0;
  double get costValue => double.tryParse(cost.text.trim()) ?? 0;
  double get lineTotal => qtyValue * costValue;
  bool get isValid => itemId != null && unitId != null && qtyValue > 0;

  Map<String, dynamic> toJson() => {
        'itemId': itemId,
        'qty': qtyValue,
        'unitId': unitId,
        'unitCost': costValue,
        if (poLineId != null) 'purchaseOrderLineId': poLineId,
      };

  void dispose() {
    qty.dispose();
    cost.dispose();
  }
}

/// Create / edit a purchase order. Returns true if saved. [existing] edits a Draft PO.
Future<bool?> showPurchaseOrderForm(BuildContext context, {PurchaseOrderDetail? existing}) =>
    showDialog<bool>(
      context: context,
      builder: (_) => _PurchaseOrderFormDialog(existing: existing),
    );

class _PurchaseOrderFormDialog extends ConsumerStatefulWidget {
  final PurchaseOrderDetail? existing;
  const _PurchaseOrderFormDialog({this.existing});

  @override
  ConsumerState<_PurchaseOrderFormDialog> createState() => _PurchaseOrderFormDialogState();
}

class _PurchaseOrderFormDialogState extends ConsumerState<_PurchaseOrderFormDialog> {
  String? _supplierId;
  DateTime _orderedAt = DateTime.now();
  DateTime? _expectedAt;
  final _notes = TextEditingController();
  final List<PoLineDraft> _lines = [];
  bool _busy = false;
  String? _error;

  bool get _isEdit => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final e = widget.existing;
    if (e != null) {
      _supplierId = e.supplierId;
      _orderedAt = e.orderedAtUtc;
      _expectedAt = e.expectedAtUtc;
      _notes.text = e.notes ?? '';
      for (final l in e.lines) {
        _lines.add(PoLineDraft(itemId: l.inventoryItemId, qty: l.qtyOrdered, cost: l.unitCost)..unitId = null);
      }
    }
    if (_lines.isEmpty) _lines.add(PoLineDraft());
  }

  @override
  void dispose() {
    _notes.dispose();
    for (final l in _lines) {
      l.dispose();
    }
    super.dispose();
  }

  double get _subtotal => _lines.fold(0.0, (a, l) => a + l.lineTotal);

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
      final api = ref.read(staffApiProvider);
      final lines = valid.map((l) => l.toJson()).toList();
      if (_isEdit) {
        await api.stockUpdatePurchaseOrder(widget.existing!.id,
            supplierId: _supplierId!, orderedAtUtc: _orderedAt, expectedAtUtc: _expectedAt,
            notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(), lines: lines);
      } else {
        await api.stockCreatePurchaseOrder(
            supplierId: _supplierId!, orderedAtUtc: _orderedAt, expectedAtUtc: _expectedAt,
            notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(), lines: lines);
      }
      ref.invalidate(stockPurchaseOrdersProvider);
      if (_isEdit) ref.invalidate(stockPurchaseOrderProvider(widget.existing!.id));
      if (mounted) {
        AppToast.show(context, _isEdit ? 'Purchase order updated' : 'Purchase order created');
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
        constraints: const BoxConstraints(maxWidth: 760, maxHeight: 720),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 16, 12, 8),
              child: Row(children: [
                Expanded(child: Text(_isEdit ? 'Edit Purchase Order' : 'New Purchase Order',
                    style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800))),
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
                  Row(children: [
                    Expanded(child: PoDateField(label: 'Ordered', value: _orderedAt, onPick: (d) => setState(() => _orderedAt = d))),
                    const SizedBox(width: 12),
                    Expanded(child: PoDateField(label: 'Expected (optional)', value: _expectedAt, onPick: (d) => setState(() => _expectedAt = d), onClear: () => setState(() => _expectedAt = null))),
                  ]),
                  const SizedBox(height: 12),
                  TextField(controller: _notes, decoration: const InputDecoration(labelText: 'Notes (optional)'), minLines: 1, maxLines: 2),
                  const SizedBox(height: 16),
                  Row(children: [
                    const Text('Lines', style: TextStyle(fontWeight: FontWeight.w700)),
                    const Spacer(),
                    TextButton.icon(
                      onPressed: _busy ? null : () => setState(() => _lines.add(PoLineDraft())),
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
                        children: [
                          for (final line in _lines)
                            PoLineEditor(
                              line: line,
                              items: itemList,
                              units: unitList,
                              currency: 'Tk',
                              onItemChanged: (it) => setState(() {
                                line.itemId = it?.id;
                                // Default unit + cost from the chosen item.
                                line.unitId ??= it?.baseUnitId;
                                if (it != null && line.unitId == null) line.unitId = it.baseUnitId;
                                if (it != null && line.cost.text.trim().isEmpty && it.avgCost > 0) {
                                  line.cost.text = it.avgCost.toString();
                                }
                              }),
                              onUnitChanged: (u) => setState(() => line.unitId = u),
                              onChanged: () => setState(() {}),
                              onRemove: _lines.length == 1 ? null : () => setState(() { _lines.remove(line); line.dispose(); }),
                            ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Align(
                    alignment: Alignment.centerRight,
                    child: Text('Subtotal: ${money(_subtotal, 'Tk')}',
                        style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
                  ),
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
                  onPressed: _busy ? null : _save,
                  child: _busy
                      ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                      : Text(_isEdit ? 'Save' : 'Create draft'),
                ),
              ]),
            ),
          ],
        ),
      ),
    );
  }
}

/// A single editable line row used by both the PO and GRN forms.
class PoLineEditor extends StatelessWidget {
  final PoLineDraft line;
  final List<StockItem> items;
  final List<StockUnit> units;
  final String currency;
  final ValueChanged<StockItem?> onItemChanged;
  final ValueChanged<String?> onUnitChanged;
  final VoidCallback onChanged;
  final VoidCallback? onRemove;

  const PoLineEditor({
    super.key,
    required this.line,
    required this.items,
    required this.units,
    required this.currency,
    required this.onItemChanged,
    required this.onUnitChanged,
    required this.onChanged,
    required this.onRemove,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Expanded(
            flex: 4,
            child: DropdownButtonFormField<String>(
              initialValue: line.itemId,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Item', isDense: true),
              items: [for (final it in items) DropdownMenuItem(value: it.id, child: Text(it.name, overflow: TextOverflow.ellipsis))],
              onChanged: (v) {
                StockItem? sel;
                for (final it in items) {
                  if (it.id == v) { sel = it; break; }
                }
                onItemChanged(sel);
              },
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            flex: 2,
            child: TextField(
              controller: line.qty,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(labelText: 'Qty', isDense: true),
              onChanged: (_) => onChanged(),
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
              onChanged: onUnitChanged,
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            flex: 2,
            child: TextField(
              controller: line.cost,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(labelText: 'Unit cost', isDense: true),
              onChanged: (_) => onChanged(),
            ),
          ),
          IconButton(
            tooltip: 'Remove line',
            onPressed: onRemove,
            icon: const Icon(Icons.remove_circle_outline, size: 20, color: Bo.danger),
          ),
        ],
      ),
    );
  }
}

class PoDateField extends StatelessWidget {
  final String label;
  final DateTime? value;
  final ValueChanged<DateTime> onPick;
  final VoidCallback? onClear;
  const PoDateField({super.key, required this.label, required this.value, required this.onPick, this.onClear});

  @override
  Widget build(BuildContext context) {
    return InputDecorator(
      decoration: InputDecoration(labelText: label, isDense: true),
      child: Row(
        children: [
          Expanded(child: Text(value == null ? '—' : shortDate(value!))),
          if (value != null && onClear != null)
            InkWell(onTap: onClear, child: const Icon(Icons.clear, size: 16, color: Bo.textSubtle)),
          InkWell(
            onTap: () async {
              final picked = await showDatePicker(
                context: context,
                initialDate: value ?? DateTime.now(),
                firstDate: DateTime(2020),
                lastDate: DateTime(2100),
              );
              if (picked != null) onPick(picked);
            },
            child: const Padding(padding: EdgeInsets.only(left: 6), child: Icon(Icons.calendar_today, size: 16)),
          ),
        ],
      ),
    );
  }
}
