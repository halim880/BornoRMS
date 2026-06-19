import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart' show money;
import 'purchase_order_form.dart';
import 'stock_api.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

/// Create / edit a goods receipt (draft). Returns true if saved.
/// [existing] edits a Draft GRN; [fromPo] prefills supplier + outstanding lines to receive against a PO.
Future<bool?> showGoodsReceiptForm(BuildContext context,
        {GoodsReceiptDetail? existing, PurchaseOrderDetail? fromPo}) =>
    showDialog<bool>(
      context: context,
      builder: (_) => _GoodsReceiptFormDialog(existing: existing, fromPo: fromPo),
    );

class _GoodsReceiptFormDialog extends ConsumerStatefulWidget {
  final GoodsReceiptDetail? existing;
  final PurchaseOrderDetail? fromPo;
  const _GoodsReceiptFormDialog({this.existing, this.fromPo});

  @override
  ConsumerState<_GoodsReceiptFormDialog> createState() => _GoodsReceiptFormDialogState();
}

class _GoodsReceiptFormDialogState extends ConsumerState<_GoodsReceiptFormDialog> {
  String? _supplierId;
  String? _purchaseOrderId;
  DateTime _receivedAt = DateTime.now();
  final _invoice = TextEditingController();
  final _notes = TextEditingController();
  final List<PoLineDraft> _lines = [];
  bool _busy = false;
  String? _error;

  bool get _isEdit => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final e = widget.existing;
    final po = widget.fromPo;
    if (e != null) {
      _supplierId = e.supplierId;
      _receivedAt = e.receivedAtUtc;
      _invoice.text = e.invoiceNo ?? '';
      _notes.text = e.notes ?? '';
      for (final l in e.lines) {
        _lines.add(PoLineDraft(itemId: l.inventoryItemId, qty: l.qty, cost: l.unitCost));
      }
    } else if (po != null) {
      _supplierId = po.supplierId;
      _purchaseOrderId = po.id;
      // Prefill one line per PO line with outstanding qty still to receive.
      for (final l in po.lines.where((l) => l.outstandingBase > 0)) {
        _lines.add(PoLineDraft(itemId: l.inventoryItemId, poLineId: l.id, qty: l.outstandingBase, cost: l.unitCost));
      }
    }
    if (_lines.isEmpty) _lines.add(PoLineDraft());
  }

  @override
  void dispose() {
    _invoice.dispose();
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
      final invoice = _invoice.text.trim().isEmpty ? null : _invoice.text.trim();
      final notes = _notes.text.trim().isEmpty ? null : _notes.text.trim();
      if (_isEdit) {
        await api.stockUpdateGoodsReceipt(widget.existing!.id,
            supplierId: _supplierId!, invoiceNo: invoice, receivedAtUtc: _receivedAt, notes: notes, lines: lines);
      } else {
        await api.stockCreateGoodsReceipt(
            supplierId: _supplierId!, invoiceNo: invoice, receivedAtUtc: _receivedAt, notes: notes,
            lines: lines, purchaseOrderId: _purchaseOrderId);
      }
      ref.invalidate(stockGoodsReceiptsProvider);
      if (_isEdit) ref.invalidate(stockGoodsReceiptProvider(widget.existing!.id));
      if (_purchaseOrderId != null) ref.invalidate(stockPurchaseOrderProvider(_purchaseOrderId!));
      if (mounted) {
        AppToast.show(context, _isEdit ? 'Goods receipt updated' : 'Goods receipt created (draft)');
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
    final fromPo = _purchaseOrderId != null;

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
                Expanded(child: Text(
                    _isEdit ? 'Edit Goods Receipt' : (fromPo ? 'Receive · ${widget.fromPo!.poNumber}' : 'New Goods Receipt'),
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
                        // Locked to the PO's supplier when receiving against a PO.
                        items: [for (final s in active) DropdownMenuItem(value: s.id, child: Text(s.name))],
                        onChanged: (_busy || fromPo) ? null : (v) => setState(() => _supplierId = v),
                      );
                    },
                  ),
                  const SizedBox(height: 12),
                  Row(children: [
                    Expanded(child: TextField(controller: _invoice, decoration: const InputDecoration(labelText: 'Invoice no (optional)'))),
                    const SizedBox(width: 12),
                    Expanded(child: PoDateField(label: 'Received', value: _receivedAt, onPick: (d) => setState(() => _receivedAt = d))),
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
                  const SizedBox(height: 8),
                  const Text('Saved as a draft. Post it from the list to raise stock and the supplier payable.',
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
