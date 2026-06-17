import 'package:flutter/material.dart';

import '../../core/models/dtos.dart';
import '../../core/theme/app_colors.dart';
import '../dashboard/widgets.dart' show money;
import 'pos_section.dart';

/// A flat cart row (no card): thumbnail tile + name/variant over unit price,
/// with the line total, a remove "×", and a stepper pill on the right.
class CartLineRow extends StatefulWidget {
  final OrderLine line;
  final String currency;
  final void Function(int delta) onQty;
  final VoidCallback onRemove;
  const CartLineRow({
    super.key,
    required this.line,
    required this.currency,
    required this.onQty,
    required this.onRemove,
  });

  @override
  State<CartLineRow> createState() => _CartLineRowState();
}

class _CartLineRowState extends State<CartLineRow> {
  bool _hoverRemove = false;

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    final text = Theme.of(context).textTheme;
    final l = widget.line;
    final variant = l.modifiers.map((m) => m.optionName).join(' · ');

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 10),
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: a.border)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // thumbnail tile
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              gradient: categoryGradient(l.menuItemId),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(Icons.restaurant, size: 18, color: categoryGlyph(l.menuItemId)),
          ),
          const SizedBox(width: 10),
          // name + unit price
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text.rich(
                  TextSpan(children: [
                    TextSpan(text: l.name, style: text.bodyLarge),
                    if (variant.isNotEmpty)
                      TextSpan(text: '  · $variant', style: text.bodySmall),
                  ]),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 2),
                Text('${money(l.unitPrice, widget.currency)} each', style: text.bodySmall),
              ],
            ),
          ),
          const SizedBox(width: 8),
          // total + remove, then stepper
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    money(l.lineTotal, widget.currency),
                    style: AppColors.priceText.copyWith(color: a.textPrimary),
                  ),
                  const SizedBox(width: 4),
                  MouseRegion(
                    onEnter: (_) => setState(() => _hoverRemove = true),
                    onExit: (_) => setState(() => _hoverRemove = false),
                    child: InkWell(
                      onTap: widget.onRemove,
                      borderRadius: BorderRadius.circular(999),
                      child: Padding(
                        padding: const EdgeInsets.all(2),
                        child: Icon(Icons.close,
                            size: 16, color: _hoverRemove ? a.danger : a.textTertiary),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              _Stepper(
                qty: l.quantity,
                onMinus: () => widget.onQty(-1),
                onPlus: () => widget.onQty(1),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Stepper extends StatelessWidget {
  final int qty;
  final VoidCallback onMinus;
  final VoidCallback onPlus;
  const _Stepper({required this.qty, required this.onMinus, required this.onPlus});

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    return Container(
      decoration: BoxDecoration(
        color: a.surfaceMuted,
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: a.border),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _btn(Icons.remove, onMinus, a),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8),
            child: Text('$qty',
                style: AppColors.priceText.copyWith(fontSize: 13, color: a.textPrimary)),
          ),
          _btn(Icons.add, onPlus, a),
        ],
      ),
    );
  }

  Widget _btn(IconData icon, VoidCallback onTap, AppColors a) => InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(999),
        child: Padding(
          padding: const EdgeInsets.all(5),
          child: Icon(icon, size: 16, color: a.textSecondary),
        ),
      );
}
