import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_exception.dart';
import '../../core/models/dtos.dart';
import '../../core/models/enums.dart';
import '../../core/providers/providers.dart';
import '../../core/widgets/format.dart';
import '../../core/widgets/snack.dart';
import 'cart_controller.dart';

void showCartPanel(BuildContext context) {
  showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.85,
      maxChildSize: 0.95,
      minChildSize: 0.5,
      builder: (ctx, scroll) => CartPanelBody(scroll: scroll, inSheet: true),
    ),
  );
}

/// The order panel content. Shown in a bottom sheet on compact widths
/// ([inSheet] = true) and embedded as the right pane on expanded widths.
class CartPanelBody extends ConsumerStatefulWidget {
  final ScrollController? scroll;
  final bool inSheet;
  const CartPanelBody({super.key, this.scroll, this.inSheet = false});
  @override
  ConsumerState<CartPanelBody> createState() => _CartPanelState();
}

class _CartPanelState extends ConsumerState<CartPanelBody> {
  final _notes = TextEditingController();
  bool _placing = false;

  @override
  void initState() {
    super.initState();
    _notes.text = ref.read(cartProvider).notes;
  }

  @override
  void dispose() {
    _notes.dispose();
    super.dispose();
  }

  Future<void> _place() async {
    final cart = ref.read(cartProvider);
    final api = ref.read(waiterApiProvider);
    setState(() => _placing = true);
    try {
      final lines = cart.lines.map((l) => l.toLineJson()).toList();
      if (cart.isEdit) {
        await api.updateOrderLines(cart.editingOrderId!, lines);
      } else {
        await api.placeOrder(
          type: cart.type,
          tableId: cart.type == OrderType.dineIn ? cart.tableId : null,
          notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
          lines: lines,
          guestCount: cart.guestCount,
          diningSessionId: cart.sessionId,
        );
      }
      ref.read(cartProvider.notifier).reset();
      await refreshConsole(ref);
      if (mounted) {
        if (widget.inSheet) {
          Navigator.pop(context);
          ref.read(selectedTabProvider.notifier).state = 0; // back to floor
        }
        showInfo(context, cart.isEdit ? 'Order updated' : 'Order placed');
      }
    } on ApiException catch (e) {
      if (mounted) showError(context, e.message);
    } finally {
      if (mounted) setState(() => _placing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cart = ref.watch(cartProvider);
    final ctrl = ref.read(cartProvider.notifier);
    final tables = ref.watch(tablesProvider).valueOrNull ?? const <RestaurantTable>[];

    return ListView(
      controller: widget.scroll,
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
      children: [
        Text(cart.isEdit ? 'Edit order' : 'Current order',
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),

        if (!cart.isEdit) ...[
          // type toggle
          SegmentedButton<OrderType>(
            segments: const [
              ButtonSegment(value: OrderType.dineIn, label: Text('Dine-in'), icon: Icon(Icons.restaurant)),
              ButtonSegment(value: OrderType.takeaway, label: Text('Takeaway'), icon: Icon(Icons.takeout_dining)),
            ],
            selected: {cart.type == OrderType.takeaway ? OrderType.takeaway : OrderType.dineIn},
            onSelectionChanged: (s) => ctrl.setType(s.first),
          ),
          const SizedBox(height: 12),
          if (cart.type == OrderType.dineIn) ...[
            Text(
              cart.tableNumber == null ? 'Table · choose one' : 'Table · ${cart.tableNumber}',
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 6),
            if (cart.sessionId != null)
              const Padding(
                padding: EdgeInsets.only(bottom: 6),
                child: Text('Session open', style: TextStyle(fontSize: 12, color: Colors.green)),
              ),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: tables.map((t) {
                final sel = cart.tableId == t.id;
                return ChoiceChip(
                  label: Text('T${t.tableNumber}'),
                  selected: sel,
                  onSelected: (_) => ctrl.setTable(t),
                );
              }).toList(),
            ),
            const SizedBox(height: 12),
          ],
        ] else
          Container(
            padding: const EdgeInsets.all(10),
            margin: const EdgeInsets.only(bottom: 12),
            decoration: BoxDecoration(
              color: const Color(0xFFCB3A1A).withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
                'Editing ${cart.editingOrderNumber} · ${cart.tableNumber ?? 'Takeaway'} — adjust items, then save'),
          ),

        // cart lines
        if (cart.lines.isEmpty)
          Container(
            padding: const EdgeInsets.all(24),
            alignment: Alignment.center,
            child: Text(
              cart.isEdit
                  ? 'All items removed — an order needs at least one item.'
                  : 'No items yet. Tap products to add.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.grey.shade600),
            ),
          )
        else
          ...cart.lines.map((l) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(l.name,
                              style: const TextStyle(fontWeight: FontWeight.w600)),
                          Text('${money(l.price, currency: l.currency)} each',
                              style: TextStyle(fontSize: 12, color: Colors.grey.shade600)),
                        ],
                      ),
                    ),
                    _QtyStepper(
                      qty: l.qty,
                      onMinus: () => ctrl.decrement(l),
                      onPlus: () => ctrl.increment(l),
                    ),
                    SizedBox(
                      width: 64,
                      child: Text(money(l.price * l.qty, twoDp: true),
                          textAlign: TextAlign.right,
                          style: const TextStyle(fontWeight: FontWeight.w700)),
                    ),
                    IconButton(
                      icon: const Icon(Icons.close, size: 18),
                      onPressed: () => ctrl.remove(l),
                    ),
                  ],
                ),
              )),

        const Divider(),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text('Total', style: TextStyle(fontSize: 16)),
            Text(money(cart.total, currency: cart.currency, twoDp: true),
                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          ],
        ),
        const SizedBox(height: 12),

        if (!cart.isEdit)
          TextField(
            controller: _notes,
            decoration: const InputDecoration(
              labelText: 'Kitchen notes (optional)',
              hintText: 'e.g. no onions, extra spicy',
              border: OutlineInputBorder(),
            ),
          ),
        const SizedBox(height: 16),
        FilledButton(
          onPressed: (!cart.canPlace || _placing) ? null : _place,
          style: FilledButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 16)),
          child: _placing
              ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
              : Text(cart.isEdit
                  ? 'Update ${cart.editingOrderNumber} · ${money(cart.total, currency: cart.currency)}'
                  : 'Place order · ${money(cart.total, currency: cart.currency)}'),
        ),
      ],
    );
  }
}

class _QtyStepper extends StatelessWidget {
  final int qty;
  final VoidCallback onMinus;
  final VoidCallback onPlus;
  const _QtyStepper({required this.qty, required this.onMinus, required this.onPlus});
  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconButton(
          visualDensity: VisualDensity.compact,
          icon: const Icon(Icons.remove_circle_outline),
          onPressed: onMinus,
        ),
        SizedBox(
          width: 24,
          child: Text('$qty',
              textAlign: TextAlign.center,
              style: const TextStyle(fontWeight: FontWeight.bold)),
        ),
        IconButton(
          visualDensity: VisualDensity.compact,
          icon: const Icon(Icons.add_circle_outline),
          onPressed: onPlus,
        ),
      ],
    );
  }
}
