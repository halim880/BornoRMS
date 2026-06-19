import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/printing/printer_settings.dart';
import '../../core/theme/app_theme.dart';
import '../dashboard/widgets.dart';
import 'pos_models.dart';
import 'pos_providers.dart';

const orderTypes = ['DineIn', 'Takeaway', 'Delivery', 'Collection', 'Waiting'];

String orderTypeLabel(String t) => switch (t) {
      'DineIn' => 'Dine In',
      _ => t,
    };

/// New-order / edit-order dialog. When [edit] is true it updates the active order.
class NewOrderDialog extends ConsumerStatefulWidget {
  final bool edit;
  const NewOrderDialog({super.key, this.edit = false});

  @override
  ConsumerState<NewOrderDialog> createState() => _NewOrderDialogState();
}

class _NewOrderDialogState extends ConsumerState<NewOrderDialog> {
  String _type = 'Takeaway';
  String? _tableId;
  final _phone = TextEditingController();
  final _name = TextEditingController();
  final _address = TextEditingController();
  final _deliveryCharge = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    if (widget.edit) {
      final d = ref.read(posControllerProvider).detail;
      if (d != null) {
        _type = d.orderType;
        _name.text = d.customerName ?? '';
        // customerPhone may be the walk-in marker; leave editable
        _address.text = '';
      }
    }
  }

  @override
  void dispose() {
    _phone.dispose();
    _name.dispose();
    _address.dispose();
    _deliveryCharge.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() { _busy = true; _error = null; });
    try {
      final c = ref.read(posControllerProvider.notifier);
      final args = (
        type: _type,
        tableId: _type == 'DineIn' ? _tableId : null,
        phone: _phone.text.trim().isEmpty ? null : _phone.text.trim(),
        name: _name.text.trim().isEmpty ? null : _name.text.trim(),
        address: _address.text.trim().isEmpty ? null : _address.text.trim(),
      );
      final charge = _type == 'Delivery' ? (double.tryParse(_deliveryCharge.text.trim()) ?? 0) : 0.0;
      if (widget.edit) {
        await c.updateMeta(type: args.type, tableId: args.tableId, customerPhone: args.phone, customerName: args.name, customerAddress: args.address);
      } else {
        await c.createOrder(type: args.type, tableId: args.tableId, customerPhone: args.phone, customerName: args.name, customerAddress: args.address, deliveryCharge: charge);
      }
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      setState(() { _busy = false; _error = e.toString(); });
    }
  }

  @override
  Widget build(BuildContext context) {
    final tablesAsync = ref.watch(posTablesProvider);
    final active = ref.watch(posActiveOrdersProvider).valueOrNull ?? const [];
    final occupied = active.where((o) => o.orderType == 'DineIn' && o.tableId != null).map((o) => o.tableId!).toSet();

    return AlertDialog(
      title: Text(widget.edit ? 'Edit order' : 'New order'),
      content: SizedBox(
        width: 420,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Wrap(
                spacing: 8,
                children: [
                  for (final t in orderTypes)
                    ChoiceChip(
                      label: Text(orderTypeLabel(t)),
                      selected: _type == t,
                      onSelected: (_) => setState(() => _type = t),
                    ),
                ],
              ),
              const SizedBox(height: 12),
              if (_type == 'DineIn')
                tablesAsync.when(
                  loading: () => const LinearProgressIndicator(),
                  error: (e, _) => Text('Tables: $e', style: const TextStyle(color: Bo.danger)),
                  data: (tables) => DropdownButtonFormField<String>(
                    initialValue: _tableId,
                    decoration: const InputDecoration(labelText: 'Table'),
                    items: [
                      for (final t in tables)
                        DropdownMenuItem(
                          value: t.id,
                          enabled: !occupied.contains(t.id),
                          child: Text('Table ${t.tableNumber} (${t.capacity})${occupied.contains(t.id) ? ' — busy' : ''}'),
                        ),
                    ],
                    onChanged: (v) => setState(() => _tableId = v),
                  ),
                ),
              const SizedBox(height: 8),
              TextField(controller: _name, decoration: const InputDecoration(labelText: 'Customer name (optional)')),
              const SizedBox(height: 8),
              TextField(controller: _phone, keyboardType: TextInputType.phone, decoration: const InputDecoration(labelText: 'Phone (optional)')),
              if (_type == 'Delivery') ...[
                const SizedBox(height: 8),
                TextField(controller: _address, decoration: const InputDecoration(labelText: 'Delivery address')),
                const SizedBox(height: 8),
                TextField(
                  controller: _deliveryCharge,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(labelText: 'Delivery charge'),
                ),
              ],
              if (_error != null) ...[
                const SizedBox(height: 10),
                Text(_error!, style: const TextStyle(color: Bo.danger, fontSize: 12)),
              ],
            ],
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: _busy ? null : () => Navigator.of(context).pop(false), child: const Text('Cancel')),
        FilledButton(
          onPressed: (_busy || (_type == 'DineIn' && _tableId == null)) ? null : _save,
          child: _busy
              ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : Text(widget.edit ? 'Save' : 'Start order'),
        ),
      ],
    );
  }
}

/// Returns the chosen variant id, or null if cancelled.
Future<PosVariant?> showVariantPicker(BuildContext context, PosProduct product) {
  return showDialog<PosVariant>(
    context: context,
    builder: (_) => SimpleDialog(
      title: Text('Choose size — ${product.name}'),
      children: [
        for (final v in product.variants)
          SimpleDialogOption(
            onPressed: () => Navigator.of(context).pop(v),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [Text(v.name), Text(money(v.price, product.currency), style: const TextStyle(fontWeight: FontWeight.w600))],
            ),
          ),
      ],
    ),
  );
}

/// Returns selected option ids, or null if cancelled.
Future<List<String>?> showOptionPicker(BuildContext context, PosProduct product, List<PosOptionGroup> groups) {
  return showDialog<List<String>>(
    context: context,
    builder: (_) => _OptionPickerDialog(product: product, groups: groups),
  );
}

class _OptionPickerDialog extends StatefulWidget {
  final PosProduct product;
  final List<PosOptionGroup> groups;
  const _OptionPickerDialog({required this.product, required this.groups});
  @override
  State<_OptionPickerDialog> createState() => _OptionPickerDialogState();
}

class _OptionPickerDialogState extends State<_OptionPickerDialog> {
  final Map<String, Set<String>> _selected = {}; // groupId -> optionIds
  String? _error;

  void _toggle(PosOptionGroup g, String optionId) {
    final set = _selected.putIfAbsent(g.id, () => {});
    setState(() {
      if (g.single) {
        set
          ..clear()
          ..add(optionId);
      } else {
        if (set.contains(optionId)) {
          set.remove(optionId);
        } else {
          if (g.maxSelections > 0 && set.length >= g.maxSelections) return;
          set.add(optionId);
        }
      }
    });
  }

  void _confirm() {
    for (final g in widget.groups) {
      final n = _selected[g.id]?.length ?? 0;
      if (n < g.minSelections) {
        setState(() => _error = 'Choose at least ${g.minSelections} from "${g.name}"');
        return;
      }
    }
    Navigator.of(context).pop(_selected.values.expand((s) => s).toList());
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text('Options — ${widget.product.name}'),
      content: SizedBox(
        width: 420,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (final g in widget.groups) ...[
                Padding(
                  padding: const EdgeInsets.only(top: 8, bottom: 4),
                  child: Text('${g.name}${g.required ? ' *' : ''}'
                      '${g.maxSelections > 1 ? '  (max ${g.maxSelections})' : ''}',
                      style: const TextStyle(fontWeight: FontWeight.w700)),
                ),
                for (final o in g.options)
                  CheckboxListTile(
                    dense: true,
                    contentPadding: EdgeInsets.zero,
                    controlAffinity: ListTileControlAffinity.leading,
                    value: _selected[g.id]?.contains(o.id) ?? false,
                    onChanged: (_) => _toggle(g, o.id),
                    title: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Flexible(child: Text(o.name)),
                        if (o.priceDelta != 0) Text('+${money(o.priceDelta, widget.product.currency)}', style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                      ],
                    ),
                  ),
              ],
              if (_error != null) ...[
                const SizedBox(height: 8),
                Text(_error!, style: const TextStyle(color: Bo.danger, fontSize: 12)),
              ],
            ],
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Cancel')),
        FilledButton(onPressed: _confirm, child: const Text('Add')),
      ],
    );
  }
}

/// Cancel-order confirmation with an optional reason. Returns true if cancelled.
Future<bool?> showCancelDialog(BuildContext context, WidgetRef ref) {
  final reason = TextEditingController();
  return showDialog<bool>(
    context: context,
    builder: (_) => AlertDialog(
      title: const Text('Cancel order?'),
      content: TextField(controller: reason, decoration: const InputDecoration(labelText: 'Reason (optional)')),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Keep')),
        FilledButton(
          style: FilledButton.styleFrom(backgroundColor: Bo.danger),
          onPressed: () async {
            await ref.read(posControllerProvider.notifier).cancel(reason: reason.text.trim().isEmpty ? null : reason.text.trim());
            if (context.mounted) Navigator.of(context).pop(true);
          },
          child: const Text('Cancel order'),
        ),
      ],
    ),
  );
}

/// Cash-drawer / shift dialog. Opens a shift (with a starting float) when none is
/// open, otherwise shows the running drawer and lets the cashier close it against a
/// physical count, surfacing the variance. Defaults to the first active cash account.
class ShiftDialog extends ConsumerStatefulWidget {
  const ShiftDialog({super.key});
  @override
  ConsumerState<ShiftDialog> createState() => _ShiftDialogState();
}

class _ShiftDialogState extends ConsumerState<ShiftDialog> {
  final _amount = TextEditingController();
  final _notes = TextEditingController();
  bool _busy = false;
  String? _error;
  DrawerCloseResult? _closed;

  static const _ccy = 'Tk';

  @override
  void dispose() {
    _amount.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _open() async {
    final value = double.tryParse(_amount.text.trim());
    if (value == null || value < 0) {
      setState(() => _error = 'Enter a valid opening float.');
      return;
    }
    setState(() { _busy = true; _error = null; });
    try {
      await ref.read(posDrawerProvider.notifier).open(
            openingBalance: value,
            notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
          );
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      setState(() { _busy = false; _error = e.toString(); });
    }
  }

  Future<void> _close(CashDrawer drawer) async {
    final value = double.tryParse(_amount.text.trim());
    if (value == null || value < 0) {
      setState(() => _error = 'Enter the counted cash amount.');
      return;
    }
    setState(() { _busy = true; _error = null; });
    try {
      final result = await ref.read(posDrawerProvider.notifier).close(
            drawer.id,
            countedBalance: value,
            notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
          );
      if (mounted) setState(() { _busy = false; _closed = result; });
    } catch (e) {
      setState(() { _busy = false; _error = e.toString(); });
    }
  }

  @override
  Widget build(BuildContext context) {
    final drawerAsync = ref.watch(posDrawerProvider);

    // Post-close summary.
    if (_closed != null) {
      final r = _closed!;
      return AlertDialog(
        title: Text('Shift closed — ${r.drawerNumber}'),
        content: SizedBox(
          width: 360,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _row('Expected in drawer', money(r.expected, _ccy)),
              _row('Counted', money(r.counted, _ccy)),
              const Divider(),
              _row(
                r.variance == 0 ? 'Balanced' : (r.variance > 0 ? 'Over' : 'Short'),
                money(r.variance, _ccy),
                tone: r.variance == 0 ? Bo.success : Bo.danger,
              ),
            ],
          ),
        ),
        actions: [
          FilledButton(onPressed: () => Navigator.of(context).pop(true), child: const Text('Done')),
        ],
      );
    }

    return drawerAsync.when(
      loading: () => const AlertDialog(content: SizedBox(height: 80, child: Center(child: CircularProgressIndicator()))),
      error: (e, _) => AlertDialog(
        title: const Text('Cash drawer'),
        content: Text('$e', style: const TextStyle(color: Bo.danger)),
        actions: [TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Close'))],
      ),
      data: (drawer) => drawer == null ? _openForm() : _closeForm(drawer),
    );
  }

  Widget _openForm() => AlertDialog(
        title: const Text('Open shift'),
        content: SizedBox(
          width: 380,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text('Start a cash drawer with the opening float. Cash payments are attributed to your open shift.',
                  style: TextStyle(color: Bo.textSubtle, fontSize: 12)),
              const SizedBox(height: 12),
              TextField(
                controller: _amount,
                autofocus: true,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(labelText: 'Opening float', prefixText: '$_ccy '),
              ),
              const SizedBox(height: 8),
              TextField(controller: _notes, decoration: const InputDecoration(labelText: 'Notes (optional)')),
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
            onPressed: _busy ? null : _open,
            child: _busy
                ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                : const Text('Open shift'),
          ),
        ],
      );

  Widget _closeForm(CashDrawer drawer) {
    final summaryAsync = ref.watch(posDrawerSummaryProvider(drawer.id));
    return AlertDialog(
      title: Text('Close shift — ${drawer.drawerNumber}'),
      content: SizedBox(
        width: 400,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _row('Opening float', money(drawer.openingBalance, _ccy)),
              _row('Cash received', money(drawer.cashReceived, _ccy)),
              if (drawer.cashPaidOut != 0) _row('Cash paid out', money(drawer.cashPaidOut, _ccy)),
              const Divider(),
              _row('Expected in drawer', money(drawer.expectedClosingBalance, _ccy), bold: true),
              summaryAsync.maybeWhen(
                data: (s) => Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (s.byMethod.isNotEmpty) ...[
                      const SizedBox(height: 8),
                      const Text('Takings by method', style: TextStyle(color: Bo.textSubtle, fontSize: 12)),
                      for (final m in s.byMethod) _row('${m.method} (${m.count})', money(m.amount, _ccy)),
                    ],
                  ],
                ),
                orElse: () => const SizedBox.shrink(),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _amount,
                autofocus: true,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(labelText: 'Counted cash', prefixText: '$_ccy '),
              ),
              const SizedBox(height: 8),
              TextField(controller: _notes, decoration: const InputDecoration(labelText: 'Notes (optional)')),
              if (_error != null) ...[
                const SizedBox(height: 10),
                Text(_error!, style: const TextStyle(color: Bo.danger, fontSize: 12)),
              ],
            ],
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: _busy ? null : () => Navigator.of(context).pop(false), child: const Text('Cancel')),
        FilledButton(
          style: FilledButton.styleFrom(backgroundColor: Bo.danger),
          onPressed: _busy ? null : () => _close(drawer),
          child: _busy
              ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Close shift'),
        ),
      ],
    );
  }

  Widget _row(String label, String value, {bool bold = false, Color? tone}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 3),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label, style: TextStyle(color: tone ?? Bo.textSubtle, fontWeight: bold ? FontWeight.w700 : null)),
            Text(value, style: TextStyle(color: tone, fontWeight: bold ? FontWeight.w700 : FontWeight.w600)),
          ],
        ),
      );
}

/// Thermal printer settings dialog.
class PrinterSettingsDialog extends ConsumerStatefulWidget {
  const PrinterSettingsDialog({super.key});
  @override
  ConsumerState<PrinterSettingsDialog> createState() => _PrinterSettingsDialogState();
}

class _PrinterSettingsDialogState extends ConsumerState<PrinterSettingsDialog> {
  final _host = TextEditingController();
  final _port = TextEditingController(text: '9100');
  final _header = TextEditingController();
  bool _init = false;

  @override
  void dispose() {
    _host.dispose();
    _port.dispose();
    _header.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final s = ref.watch(printerSettingsProvider);
    if (!_init && s.hasValue) {
      _host.text = s.value!.host;
      _port.text = s.value!.port.toString();
      _header.text = s.value!.headerName;
      _init = true;
    }
    return AlertDialog(
      title: const Text('Thermal printer'),
      content: SizedBox(
        width: 380,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(controller: _host, decoration: const InputDecoration(labelText: 'Printer IP (blank = use PDF)', hintText: '192.168.1.50')),
            const SizedBox(height: 8),
            TextField(controller: _port, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Port')),
            const SizedBox(height: 8),
            TextField(controller: _header, decoration: const InputDecoration(labelText: 'Receipt header')),
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Close')),
        FilledButton(
          onPressed: () async {
            await ref.read(printerSettingsProvider.notifier).save(
                  host: _host.text,
                  port: int.tryParse(_port.text) ?? 9100,
                  headerName: _header.text.trim().isEmpty ? 'BornoBit Restaurant' : _header.text,
                );
            if (context.mounted) Navigator.of(context).pop();
          },
          child: const Text('Save'),
        ),
      ],
    );
  }
}
